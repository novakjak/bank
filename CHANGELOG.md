# Changelog

Všechny významné změny tohoto projektu budou zaznamenány v tomto souboru.
Formát vychází z [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## 2026-01-25 - Martin Šilar

### Přidáno

- Unit a integrační testy
- InMemoryAccountStorage pro testovací účely
- README.md

### Změněno

- Diagramy
- Design dokumentace

---

## 2026-01-24 - Martin Šilar

### Přidáno

- Monitoring statistik node (snapshoty + command metrics ukládané do MySQL)
- Webové rozhraní, které zobrazuje statistiky
- Možnost vypnout node přes web

---

## 2026-01-23 - Martin Šilar

### Přidáno

- Počáteční projektová dokumentace (analýza a návrh)
- Soubor CHANGELOG.md
- Ukládání dat do db (mysql) nebo csv (fallback)
- DB schéma

### Změněno

- Reorganizace struktury projektu do složek
- Logger zapisuje informace a chyby do různých souborů
