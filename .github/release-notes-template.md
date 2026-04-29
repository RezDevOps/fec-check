<!--
  En-tête publié en haut de chaque Release GitHub fec-check.
  Le détail des changements de la version est dans CHANGELOG.md, présent
  dans chaque archive. Ce fichier reste générique pour ne pas dupliquer
  l'information et limiter le risque de divergence.
-->

## Téléchargement

Choisissez l'archive correspondant à votre système :

- `fec-check-<version>-win-x64.zip` — Windows 10/11 x64
- `fec-check-<version>-linux-x64.tar.gz` — Linux x64 (Ubuntu, Debian, RHEL…)
- `fec-check-<version>-osx-arm64.tar.gz` — macOS Apple Silicon (M1/M2/M3/M4)

Les binaires sont **self-contained Native AOT** — aucun .NET requis sur le poste cible.

## Vérification

```sh
# 1. Vérifier l'intégrité (toutes plateformes)
sha256sum -c SHA256SUMS

# 2. Vérifier l'origine via cosign (sigstore keyless)
cosign verify-blob \
  --bundle SHA256SUMS.sig \
  --certificate-identity-regexp '^https://github.com/RezDevOps/fec-check/' \
  --certificate-oidc-issuer https://token.actions.githubusercontent.com \
  SHA256SUMS
```

Un SBOM CycloneDX (`*.cdx.json`) est joint pour audit de la chaîne de dépendances.

## Détail des changements

Voir [`CHANGELOG.md`](https://github.com/RezDevOps/fec-check/blob/main/CHANGELOG.md).

## Code de retour

| Code | Sens                                  |
|------|---------------------------------------|
| 0    | Conforme                              |
| 1    | Conforme avec avertissements          |
| 2    | Non conforme                          |
| 3    | Erreur d'I/O                          |
| 64   | Usage incorrect                       |
