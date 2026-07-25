# Rivage — Plateforme de gestion des formations

**Rivage** est une application web ASP.NET Core (.NET 8) de gestion des formations : authentification par rôles, administration CRUD, parcours apprenant (inscription, progression, quiz, certificat), espace formateur, et **formateur IA** (Anam.ai + fallback mock audio).

## Thème & branding

**Métaphore :** le littoral — chaque apprenant part du *rivage* de la découverte pour gagner le *large* de la maîtrise.

| Élément | Choix |
|--------|--------|
| Nom | **Rivage** |
| Palette | Encre marine `#0B2E36`, écume `#E8F3F1`, sable `#F3EBE0`, signal corail `#D9764E` |
| Typo | Fraunces (titres) + Manrope (texte) |
| UI | Français |

## Stack

- ASP.NET Core 8 MVC + Razor
- ASP.NET Core Identity (rôles : `Admin`, `Trainer`, `Learner`)
- EF Core 8 + **PostgreSQL 16** (migrations réelles; équivalent SQL Server du brief, plus fiable sur Docker/Railway)
- Anam.ai (`IAiAvatarService`) + mock TTS navigateur
- Docker Compose (dev + tests)
- Dockerfile multi-stage (prod / Railway)

### Architecture

```
src/
  Rivage.Domain/          Entités, enums, IAiAvatarService
  Rivage.Infrastructure/  EF Core, Identity, seed, Anam/Mock, services métier
  Rivage.Web/             MVC, vues, API avatar
tests/
  Rivage.Tests/           Unit + integration (WebApplicationFactory)
```

## Mapping des exigences

| Exigence | Implémentation |
|----------|----------------|
| Authentification / Inscription | `AccountController` + Identity |
| Interface d'administration | `/Admin` → `AdminDashboardController` |
| Gestion des formations | `Admin/FormationsController` + vues |
| Gestion des formateurs | `Admin/TrainersController` |
| Gestion des catégories | `Admin/CategoriesController` |
| Gestion des modules / cours | `Admin/ModulesController` |
| Modalités Cours / Exercices / Quiz | `ModuleContentType` + écrans module apprenant |
| Inscription des apprenants | `LearnerController.Enroll` + `EnrollmentService` |
| Formateur IA | `IAiAvatarService` → `AnamAiAvatarService` / `MockAiAvatarService` + `api/ai-avatar` |

## Prérequis

- **Docker Engine + Docker Compose** uniquement (workflow documenté)
- Votre utilisateur doit pouvoir lancer Docker sans erreur `permission denied` :

```bash
sudo usermod -aG docker "$USER"
# puis déconnexion/reconnexion ou :
newgrp docker
docker ps   # doit fonctionner
```

## Démarrage local (Docker)

```bash
cd /chemin/vers/dotnetproject
cp .env.example .env   # si besoin

# Tout Docker (web + Postgres) — publish self-contained puis compose
export PATH="$HOME/.dotnet:$PATH"
dotnet publish src/Rivage.Web/Rivage.Web.csproj -c Release -r linux-x64 --self-contained true -o publish
docker compose up --build -d

# App : http://localhost:5011
docker compose ps
```

> Note: le pull des images Microsoft (`mcr.microsoft.com`) échoue parfois (DNS Docker).  
> `Dockerfile.dockeronly` contourne ça en s’appuyant sur `postgres:16` déjà local + binaire self-contained.  
> Pour réparer le DNS Docker (images SDK officielles) : `./scripts/fix-docker-dns.sh`

### Comptes démo (seed)

| Rôle | Email | Mot de passe |
|------|-------|--------------|
| Admin | `admin@rivage.local` | `Rivage@Admin2026!` |
| Formateur | `formateur@rivage.local` | `Rivage@Trainer2026!` |
| Apprenant | `apprenant@rivage.local` | `Rivage@Learner2026!` |

Inscription publique → rôle **Learner**. La case « Je confirme que mon adresse email est valide » marque `EmailConfirmed` **sans SMTP**.

### Commandes utiles

```bash
# Jour / jour
docker compose up
docker compose down

# Rebuild web
docker compose up --build web

# Migrations manuelles (dans le conteneur)
docker compose exec web dotnet ef database update \
  --project src/Rivage.Infrastructure/Rivage.Infrastructure.csproj \
  --startup-project src/Rivage.Web/Rivage.Web.csproj

# Tests (conteneur + SQL Server jetable)
docker compose -f docker-compose.test.yml run --rm tests

# Image production
docker build -t rivage-web:prod -f Dockerfile .

# Reset base (volume SQL)
docker compose down -v
```

Au démarrage, l'app exécute `MigrateAsync` + seed automatiquement.

## Formateur IA (Anam.ai)

1. Le serveur appelle `POST https://api.anam.ai/v1/auth/session-token` avec `ANAM_API_KEY` (jamais exposée au navigateur).
2. Le client reçoit un `sessionToken` et utilise `@anam-ai/js-sdk` pour le flux vidéo/audio.
3. **Sans clé** (ou si Anam échoue) : `MockAiAvatarService` fournit un script de narration ; le navigateur lit à voix haute via `speechSynthesis` (fr-FR). Les questions texte reçoivent une réponse pédagogique mock.

Variables :

| Variable | Requis | Rôle |
|----------|--------|------|
| `ANAM_API_KEY` | Non* | Clé API Anam (`*` mock si vide) |
| `ANAM_AVATAR_ID` | Non | Avatar (défaut Cara) |
| `ANAM_AVATAR_MODEL` | Non | ex. `cara-4` |
| `ANAM_VOICE_ID` | Non | Voix |
| `ANAM_LLM_ID` | Non | LLM persona |

## Variables d'environnement

| Nom | Requis | Exemple / défaut | But |
|-----|--------|------------------|-----|
| `POSTGRES_USER` | Oui (compose) | `rivage` | Utilisateur Postgres |
| `POSTGRES_PASSWORD` | Oui (compose) | `Rivage_Pg_S3cure!` | Mot de passe Postgres |
| `POSTGRES_DB` | Oui (compose) | `RivageDb` | Nom de la base |
| `CONNECTION_STRING` | Oui | `Host=db;Port=5432;...` | Chaîne EF Core (réseau Docker) |
| `CONNECTION_STRING_HOST` | Non | `Host=localhost;Port=5433;...` | Chaîne si l'app tourne sur l'hôte |
| `APP_PORT` | Non | `5011` | Port hôte → app |
| `ASPNETCORE_ENVIRONMENT` | Non | `Development` | Environnement |
| `ASPNETCORE_URLS` | Non | `http://+:8080` | Écoute conteneur |
| `PORT` | Non (Railway) | injecté | Remappé dans `Program.cs` |
| `SEED_ADMIN_EMAIL` | Non | `admin@rivage.local` | Admin seed |
| `SEED_ADMIN_PASSWORD` | Non | `Rivage@Admin2026!` | Mot de passe admin seed |
| `ANAM_*` | Non | voir ci-dessus | Avatar IA |

## Déploiement Railway (plus tard)

Préparé :

- `Dockerfile` multi-stage (runtime non-root, port 8080)
- `ForwardedHeaders` pour le proxy HTTPS
- Prise en charge de `PORT`

À fournir de votre côté (quand vous le souhaitez) :

1. Projet Railway + base SQL Server (ou Azure SQL) accessible
2. Variables d'environnement (`CONNECTION_STRING`, `ANAM_API_KEY`, seed admin…)
3. Confirmation pour déclencher le deploy

GitHub : **pas de push** tant que vous n'avez pas créé le dépôt et demandé explicitement.

## Tests

```bash
# Via Docker (recommandé)
docker compose -f docker-compose.test.yml run --rm tests

# Résultat attendu : 18 tests OK (unitaires + intégration)
```

## Limites / suites possibles

- Confirmation email **simulée** (checkbox), pas d'envoi SMTP
- Notation d'exercice simplifiée (score forfaitaire si réponse non vide)
- Analytics formateur : liste des tentatives, pas de graphiques avancés
- Certificat : page imprimable (pas de PDF serveur)
- Anam : dépend de la disponibilité/quota de la clé ; mock toujours prêt
- PostgreSQL utilisé (équivalent du brief) pour fiabilité Docker/Railway ; le pull SQL Server MCR échouait en DNS sur cette machine

## Licence

Projet académique — usage pédagogique.
