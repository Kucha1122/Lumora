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

Hostname `k3s-server.tail11891a.ts.net` już obsługuje CyclingForge na porcie 443
(`/login`, `/dashboard`). Żeby uniknąć kolizji ścieżek, Lumora Api wystawiamy na
osobnym porcie HTTPS tego samego hosta zamiast dzielić ścieżkę `/` z CyclingForge:

```
tailscale serve --bg --https=8443 http://localhost:<nodePort-lub-clusterIP-forward>/
```

W k3s najprościej przez `kubectl port-forward` na stałe (np. jako systemd unit) albo
przez dodanie Service typu `NodePort` i wskazanie na niego w `tailscale serve`. Do ustalenia
razem — na razie `k8s/api.yaml` definiuje `lumora-api` jako `ClusterIP`; jeśli chcesz to
odsłonić przez Tailscale, powiedz i dopiszemy `NodePort` albo mały `nginx`/`socat` sidecar
zamiast strzelać teraz w ciemno na konfigurację hosta.

Po wystawieniu klient Windows łączy się przez `https://k3s-server.tail11891a.ts.net:8443`
zamiast lokalnego adresu — do zmiany w `src/Client.Desktop/appsettings.json`
(`ServerBaseAddress`).

## 5. Weryfikacja

```
kubectl -n lumora get pods
kubectl -n lumora logs deployment/lumora-api
curl http://<pod-lub-service>/healthz
```
