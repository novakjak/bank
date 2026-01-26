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

## 2026-01-20 - Jakub Novák

## Přidáno

- Implementace proxyování zpráv a odpovědí

## Změněno

- Program povoluje připojení skrz loopback adrssu


## 2026-01-19 - Jakub Novák

## Změněno

- Přidaná logika pro zpracování odpovědí
- Opraveno přerušování tasků


## 2026-01-18 - Jakub Novák

## Přidáno

- Logování
- Konfigurace z konfiguračního souboru
- Timeouty na připojení

## Změněno

- Oprava chyb v parsování zpráv


## 2026-1-17 - Jakub Novák

### Přidáno

- Vytvořen projekt
- Přidání funkcionality přijímání zpráv a zasílání odpovědí
- Vytvořena logika pro parsování zpráv
