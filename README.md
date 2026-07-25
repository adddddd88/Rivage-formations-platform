# Rivage — Plateforme de gestion des formations

## Description

**Rivage** est une application web de gestion des formations développée en **ASP.NET Core (.NET 8)** pour un projet de fin d’études.

Elle permet de gérer le catalogue de formations, les formateurs, les catégories, les modules (cours, exercices, quiz), les inscriptions des apprenants, le suivi de progression, ainsi qu’un **formateur IA** (intégration Anam.ai avec mode de secours audio dans le navigateur).

**Thème :** métaphore du littoral — partir du *rivage* de la découverte vers le *large* de la maîtrise. Interface en français.

### Fonctionnalités

- Authentification / inscription (rôles : Admin, Formateur, Apprenant)
- Interface d’administration (CRUD formations, formateurs, catégories, modules, quiz)
- Catalogue public avec recherche et filtres
- Parcours apprenant : inscription, modules ordonnés, exercices, quiz, certificat
- Espace formateur : formations assignées, inscrits, résultats de quiz
- Formateur IA : présentation orale des modules et réponses aux questions

### Stack technique

| Élément | Technologie |
|--------|-------------|
| Backend / UI | ASP.NET Core 8 MVC + Razor |
| Auth | ASP.NET Core Identity |
| Base de données | PostgreSQL 16 + EF Core (migrations) |
| Conteneurs | Docker Compose |
| Avatar IA | Anam.ai (`IAiAvatarService` + fallback mock) |

### Structure du projet

```
src/Rivage.Domain/           Entités et interfaces
src/Rivage.Infrastructure/   EF Core, Identity, seed, services
src/Rivage.Web/              Contrôleurs, vues, API
tests/Rivage.Tests/          Tests unitaires et d’intégration
```

---

## Prérequis

- Docker Engine + Docker Compose
- SDK .NET 8 (pour la commande `dotnet publish` avant le build Docker)

Sous Linux, si `docker` indique *permission denied* :

```bash
sudo usermod -aG docker "$USER"
newgrp docker
```

---

## Comment lancer le projet

```bash
git clone git@github.com:adddddd88/Rivage-formations-platform.git
cd Rivage-formations-platform

cp .env.example .env
# Éditer .env si besoin (mot de passe Postgres, clé Anam, etc.)

export PATH="$HOME/.dotnet:$PATH"

dotnet publish src/Rivage.Web/Rivage.Web.csproj \
  -c Release -r linux-x64 --self-contained true -o publish

docker compose up --build -d
```

Application : **http://localhost:5011**

Vérifier les conteneurs :

```bash
docker compose ps
```

Arrêter :

```bash
docker compose down
```

Réinitialiser la base (seed à nouveau) :

```bash
docker compose down -v
docker compose up --build -d
```

Au démarrage, l’application applique les migrations EF Core et charge les données de démonstration automatiquement.

---

## Comptes de démonstration

| Rôle | Email | Mot de passe |
|------|-------|--------------|
| Admin | `admin@rivage.local` | `Rivage@Admin2026!` |
| Formateur | `formateur@rivage.local` | `Rivage@Trainer2026!` |
| Apprenant | `apprenant@rivage.local` | `Rivage@Learner2026!` |

L’inscription publique crée un compte **Apprenant**.  
La confirmation d’email est simulée (case à cocher), sans envoi SMTP.

---

## Variables d’environnement principales

Copier `.env.example` vers `.env` (ne jamais committer `.env`).

| Variable | Rôle |
|----------|------|
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | Identifiants Postgres |
| `CONNECTION_STRING` | Chaîne EF Core (réseau Docker : `Host=db;...`) |
| `APP_PORT` | Port hôte (défaut `5011`) |
| `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD` | Compte admin initial |
| `ANAM_API_KEY` | Clé Anam.ai (optionnel ; sans clé → mode mock audio) |

---

## Formateur IA (bref)

- Avec `ANAM_API_KEY` : le serveur crée un session token Anam ; le navigateur affiche l’avatar.
- Sans clé : narration et réponses via le mode mock (`speechSynthesis` du navigateur).

---

## Tests

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test tests/Rivage.Tests/Rivage.Tests.csproj
```

Ou via Docker :

```bash
docker compose -f docker-compose.test.yml run --rm tests
```

---

## Licence

Projet académique — usage pédagogique.
