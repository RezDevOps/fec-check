# Politique de sécurité

> Ce document décrit comment signaler une vulnérabilité dans `fec-check`, et
> comment les binaires publiés sont signés. Il est délibérément court : le
> projet est un utilitaire CLI déterministe, sans réseau, sans persistance.
> La surface d'attaque réelle est limitée au parsing du fichier d'entrée.

## Versions supportées

Les patches de sécurité sont appliqués sur la **dernière version mineure
publiée** uniquement. Pas de support des branches antérieures au MVP : tant
que le projet est sur `v1.x`, les correctifs sortent en `v1.x.y`.

## Signaler une vulnérabilité

Pour tout signalement **non public** :

- Ouvrir un *security advisory* privé via
  [GitHub Security Advisories](https://github.com/RezDevOps/fec-check/security/advisories/new),
- ou écrire à `r.rezaire@gmail.com` avec l'objet `[fec-check] security`.

Pour les bugs et anomalies non sensibles : passer par les
[Issues GitHub](https://github.com/RezDevOps/fec-check/issues) habituelles.

Engagement d'accusé de réception sous 72 h. Pas de bug bounty, pas
d'engagement de délai de correction au stade MVP — seulement de la
transparence sur ce qui est fait, quand, et pourquoi.

## Surface d'attaque

`fec-check` lit un fichier passé en argument et écrit (au plus) deux
rapports en local. Aucun appel réseau, aucune télémétrie, aucune
exécution de code embarqué dans le FEC.

Les vecteurs plausibles :

- **Crash sur fichier malveillant** — un FEC pathologique conçu pour
  exploiter le parser. Mitigations : streaming byte-level, allocations
  bornées, encodages explicitement allow-listés (cf. `EncodingDetector`),
  pas de regex sur entrée non bornée. Toute lecture qui dépasse les
  garanties d'allocation est un bug — à signaler.
- **Path traversal sur les options `--output-md` / `--output-json`** — le
  CLI n'accepte que les chemins fournis par l'utilisateur lui-même. Pas
  de surface ici tant que l'outil n'est pas exposé en service.

## Intégrité et provenance des binaires

Chaque release publie, à côté des binaires :

- `SHA256SUMS` : empreintes SHA-256 de tous les artefacts (archives + SBOM).
- `SHA256SUMS.sig` : signature **sigstore** générée en mode *keyless*
  (cosign + OIDC GitHub Actions). Vérifiable par tout le monde sans
  dépendre d'une clé privée détenue par RezDevOps.
- `fec-check-<version>-sbom.cdx.json` : SBOM CycloneDX listant la chaîne
  de dépendances NuGet effective.
- Attestation SLSA de provenance ([attest-build-provenance](https://github.com/actions/attest-build-provenance))
  vérifiable via `gh attestation verify <fichier> --owner RezDevOps`.

Vérification rapide :

```sh
sha256sum -c SHA256SUMS

cosign verify-blob \
  --bundle SHA256SUMS.sig \
  --certificate-identity-regexp '^https://github.com/RezDevOps/fec-check/' \
  --certificate-oidc-issuer https://token.actions.githubusercontent.com \
  SHA256SUMS
```

`fec-check.exe` n'est **pas** signé en Authenticode (Windows SmartScreen
peut afficher un avertissement au premier lancement). Cette signature
sera ajoutée si une demande client réelle le justifie.

## Dépendances tierces

Le projet maintient une seule dépendance NuGet en plus de la BCL .NET 8 :
`System.Text.Encoding.CodePages` (Microsoft, MIT, requis pour décoder
ISO-8859-15 hors Windows). Toute nouvelle dépendance sera justifiée dans
le `README.md` (§ Dépendances) et listée dans le SBOM publié à chaque
release.
