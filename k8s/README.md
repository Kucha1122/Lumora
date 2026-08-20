# Wdrożenie Lumora Api (k3s)

Pipeline (`.github/workflows/deploy.yml`) buduje obraz, pushuje do `ghcr.io` i robi rollout
przy każdym pushu na `main`. Poniższe kroki trzeba wykonać ręcznie, raz, zanim pierwszy deploy
zadziała — pipeline świadomie ich nie robi (sekrety i pull-secret nie powinny przechodzić
przez CI).

## 1. Namespace + pull secret

```
kubectl apply -f k8s/namespace.yaml

# Skopiuj istniejący ghcr-pull z innego projektu (np. cyclingforge) do namespace lumora:
kubectl get secret ghcr-pull -n cyclingforge -o yaml \
  | sed 's/namespace: cyclingforge/namespace: lumora/' \
  | kubectl apply -f -
```

## 2. Sekrety aplikacji

Skopiuj `k8s/secrets.example.yaml` do `k8s/secrets.yaml` (ten plik jest w `.gitignore`,
nigdy nie trafia do repo), wstaw prawdziwe hasło do SQL Servera i losowy `RoomAuth__Pepper`
(min. 32 znaki), potem:

```
kubectl apply -f k8s/secrets.yaml
```

## 3. Baza danych — migracje

Program.cs stosuje migracje automatycznie tylko w `Development`. W produkcji uruchom
migracje ręcznie przed pierwszym deployem (i po każdej zmianie schematu):

```
dotnet ef database update -p src/Infrastructure -s src/Api ^
  --connection "Server=192.168.50.202,1433;Database=Lumora;User Id=sa;Password=<realne_haslo>;TrustServerCertificate=True"
```

## 4. Publiczny dostęp przez Tailscale

Ten sam wzorzec co CyclingForge: Tailscale Funnel przekazuje cały ruch na hostname
`k3s-server.tail11891a.ts.net` prosto do Traefika (wbudowany Ingress controller k3s), a to
Traefik — na podstawie obiektów `Ingress` z różnych namespace'ów — rozdziela ruch dalej po
ścieżce. Funnel wystawia jeden hostname na node, nie per-projekt subdomenę, więc każdy
serwis dostaje własną ścieżkę zamiast własnej domeny.

CyclingForge zajmuje `/` (bez `host:` w regule Ingress = łapie wszystko, co nie trafi
gdzie indziej). Żeby uniknąć kolizji, `lumora-api` dostaje własny prefiks: `/lumora-api`.
Ponieważ endpointy Api są zarejestrowane pod `/` (bez `PathBase`), `k8s/api.yaml` dokłada
`Middleware` (`stripPrefix`) w Traefiku, który zdejmuje `/lumora-api` z requestu zanim ten
trafi do poda — więc kod aplikacji nic o tym prefiksie nie wie, widzi zwykłe `/rooms`,
`/clipboard`, `/hub/clipboard` itd. `Middleware` + `Ingress` są już w `k8s/api.yaml`
(`apiVersion: traefik.containo.us/v1alpha1` — dopasowane do wersji Traefika w tym klastrze,
sprawdzonej przez `kubectl api-resources | grep -i middleware`).

To wszystko idzie przez zwykły `kubectl apply` w pipeline — nic dodatkowego nie trzeba
ręcznie konfigurować w samym Tailscale (Funnel już jest podpięty pod Traefika dla
CyclingForge, więc obejmuje to też Lumorę automatycznie).

Po pierwszym udanym deployu klient Windows powinien łączyć się przez
`https://k3s-server.tail11891a.ts.net/lumora-api` zamiast lokalnego adresu — do zmiany
w `src/Client.Desktop/appsettings.json` (`ServerBaseAddress`), dopiero gdy potwierdzimy,
że endpoint faktycznie odpowiada z zewnątrz (patrz weryfikacja niżej).

## 5. Weryfikacja

```
kubectl -n lumora get pods
kubectl -n lumora logs deployment/lumora-api
curl http://<pod-lub-service>/healthz     # z hosta/wewnątrz klastra

curl https://k3s-server.tail11891a.ts.net/lumora-api/healthz   # z zewnątrz, po deployu Ingressa
```
