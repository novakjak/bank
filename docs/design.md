# P2P Bankovní uzel – návrh řešení

**Autor:** Martin Šilar  
**Škola:** SPŠE Ječná  
**Datum:** 17. 01. 2026  
**Typ práce:** Školní projekt  

## 1. Účel návrhu

Tento dokument popisuje návrh řešení bankovního uzlu fungujícího v decentralizované P2P síti na základě provedené analýzy.
Cílem návrhu je stanovit architekturu aplikace, rozdělení do vrstev, základní datové toky, způsob ukládání dat a komunikaci mezi jednotlivými bankovními uzly.

Návrh slouží jako podklad pro následnou implementaci, testování a obhajobu projektu.

## 2. Architektura aplikace

Aplikace je navržena jako **vícevrstvá (four-tier) architektura**, která odděluje síťovou komunikaci, prezentační logiku, aplikační logiku a ukládání dat.
Toto rozdělení zajišťuje přehlednost, snadnější údržbu a možnost budoucího rozšiřování.

Následující diagram znázorňuje bankovní uzel v kontextu okolních aktérů a externích systémů, se kterými komunikuje.
![Context Diagram](diagrams/context-diagram.svg)

### 2.1 Přehled vrstev

#### 1. Connection Layer

- zajišťuje příjem a správu TCP spojení
- obsluhuje více klientů paralelně
- řeší timeouty síťové komunikace
- vytváří odchozí TCP spojení na jiné bankovní uzly (proxy režim)

Každé příchozí spojení je obsluhováno samostatným pracovním vláknem (worker).

#### 2. Presentation Layer

- zajišťuje komunikaci s klientem
- přijímá textové příkazy a odesílá odpovědi
- neobsahuje aplikační ani databázovou logiku
- formátuje odpovědi podle definovaného protokolu (BC, AC, AD, ER apod.)

#### 3. Business Layer

- obsahuje hlavní aplikační logiku banky
- zpracovává jednotlivé příkazy
- rozhoduje, zda je příkaz lokální nebo proxy
- kontroluje platnost operací a stav účtů
- komunikuje výhradně s persistence vrstvou

#### 4. Persistence Layer

- zajišťuje ukládání a načítání dat
- poskytuje jednotné rozhraní pro práci s uložištěm
- podporuje více způsobů ukládání dat (MySQL / JSON)

Vnitřní struktura aplikace a vztahy mezi jednotlivými komponentami jsou znázorněny na následujícím diagramu.
![Component Diagram](diagrams/component-diagram.svg)

Fyzické rozmístění systému, běhové prostředí a komunikační vazby jsou znázorněny v následujícím deployment diagramu.
![Deployment Diagram](diagrams/deployment-diagram.svg)

## 3. Zpracování příkazů

### 3.1 Parser a Command Pattern

Příchozí příkazy jsou zpracovávány pomocí parseru, který:

- ověřuje syntaxi příkazu
- rozpoznává typ příkazu
- vytváří odpovídající objekt příkazu

Každý příkaz je implementován pomocí **Command Pattern**, což umožňuje oddělení jednotlivých operací a přehledné řízení aplikační logiky.
Zpracování probíhá v rámci pracovního vlákna obsluhujícího konkrétní TCP spojení.

Následující sekvenční diagram ukazuje tok zpracování lokálního příkazu, který je obsluhován přímo aktuálním bankovním uzlem.
![Sequence Diagram – Local Command](diagrams/sequence-local-command.svg)

### 3.2 Omezení zátěže (Soft limits)

Pro ochranu uzlu proti nadměrnému zatížení je implementováno omezení počtu příkazů na jedno TCP spojení.

- Connection Layer sleduje počet příkazů v definovaném časovém intervalu
- při překročení limitu je příkaz odmítnut chybovou odpovědí ER

Tento mechanismus chrání uzel proti zahlcení a je zahrnut do interního monitoringu.

## 4. Proxy komunikace mezi bankami

Pokud příkaz obsahuje jiný bankovní kód (IP adresu), než má aktuální uzel:

1. Business Layer rozpozná cizí banku
2. Connection Layer naváže TCP spojení s cílovým uzlem
3. příkaz je přeposlán vzdálenému uzlu
4. odpověď je vrácena původnímu klientovi

Tento přístup umožňuje přímou komunikaci bez centrálního serveru.

Tok proxy příkazu mezi dvěma bankovními uzly v P2P síti je znázorněn na následujícím sekvenčním diagramu.
![Sequence Diagram – Proxy Command](diagrams/sequence-proxy-command.svg)

## 5. Ukládání dat (Persistence)

### 5.1 Strategie ukládání dat

Pro ukládání dat je použit **Strategy Pattern**:

- primární strategie: databáze MySQL
- záložní strategie: ukládání do JSON souborů

Při spuštění aplikace je ověřena dostupnost databáze a zvolena odpovídající strategie.
Aplikační logika pracuje pouze s abstraktním rozhraním persistence vrstvy.

### 5.2 Datový model (MySQL)

Databáze obsahuje minimálně tabulku:

#### `accounts`

- `account_number` – číslo účtu
- `bank_code` – IP adresa banky
- `balance` – aktuální zůstatek

### 5.3 Databázové připojení

Připojení k databázi MySQL je řešeno samostatnou komponentou:

- centralizovaná správa připojení
- oddělení konfigurace od aplikační logiky
- možnost snadné změny databázového serveru

### 5.4 Společná strategie ukládání dat

Persistence vrstva používá jednotný přístup pro ukládání:

- aplikačních dat
- provozních snapshotů

Při výpadku MySQL dochází k automatickému přepnutí na souborové ukládání (JSON).
Přechod je transparentní pro aplikační logiku.

### 5.5 Ukládání provozních snapshotů v databázi

Provozní snapshoty jsou v databázi ukládány do samostatné tabulky určené pro
monitorovací a diagnostická data bankovního uzlu.

Tabulka slouží k dlouhodobému uchovávání informací o stavu uzlu v čase a umožňuje
zpětnou analýzu jeho chování během provozu.

Ukládané informace zahrnují zejména:

- čas vytvoření snapshotu
- dobu běhu aplikace (uptime)
- aktuální health state uzlu
- základní statistiky provozu (počet spojení, příkazů, chyb)
- informaci o aktuálně použité strategii ukládání dat

## 6. Logování a monitoring

### 6.1 Logování

Aplikace používá dva logovací soubory:

- **info log** – běžný provoz, příkazy, proxy komunikace
- **error log** – chyby, timeouty, selhání persistence

Logování je realizováno do textových souborů.

### 6.2 Interní monitoring uzlu

Monitoring je realizován jako samostatný background worker a slouží ke sběru provozních dat, nikoliv k jejich ukládání.

Sledované údaje:

- uptime serveru
- počet aktivních TCP spojení
- celkový počet zpracovaných příkazů
- počet proxy volání
- počet chyb

### 6.3 Command metrics

Monitoring dále sleduje metriky jednotlivých příkazů:

- počet zpracování příkazů podle typu
- průměrnou dobu zpracování
- počet chyb na příkaz

Metriky jsou aktualizovány v Business Layeru.

### 6.4 Provozní snapshoty

Provozní snapshot představuje strukturovaný záznam stavu uzlu v daném čase a je vytvářen periodicky background workerem.

Snapshot obsahuje:

- timestamp vytvoření
- uptime aplikace
- aktuální health state
- počet aktivních TCP spojení
- celkový počet zpracovaných příkazů
- rozdělení příkazů podle typu
- počet proxy volání
- počet chyb od posledního snapshotu
- aktuálně použitou persistence strategii

Snapshoty jsou ukládány prostřednictvím persistence vrstvy.

### 6.5 Vlastní správa chyb (Custom error handling)

Aplikace používá centralizovaný mechanismus správy chyb.

Každá chyba obsahuje:

- typ chyby (NETWORK, PERSISTENCE, PROTOCOL, BUSINESS)
- technickou zprávu pro logy
- uživatelskou zprávu (ER \<message>)
- závažnost chyby

Chybová logika rozhoduje o logování, metrikách a vlivu na stav uzlu.

### 6.6 Interní health state uzlu

Uzel udržuje interní stav zdraví:

- **OK** – plně funkční
- **DEGRADED** – omezená funkčnost
- **ERROR** – kritický stav

Health state je zaznamenáván do snapshotů a ovlivňuje chování systému.

### 6.7 Graceful degradation

Při částečném selhání systém pokračuje v omezeném režimu:

- výpadek MySQL → přepnutí na JSON
- nestabilní síť → omezení proxy komunikace
- zvýšená chybovost → zpřísnění limitů

### 6.8 Graceful shutdown

Při ukončení aplikace:

1. je zastaven příjem nových spojení
2. aktivní spojení jsou korektně dokončena
3. workery jsou ukončeny
4. je vytvořen finální snapshot
5. persistence vrstva je korektně uzavřena

## 7. Konfigurace systému

Konfigurace je uložena v samostatném souboru a zahrnuje:

- IP adresu uzlu
- port
- timeouty
- nastavení databáze MySQL

## 8. Znovupoužití existujícího kódu

Projekt znovu využívá:

- Command Pattern
- worker / thread model
- správu databázového připojení
- logovací mechanismus
- konfigurační řešení

## 9. Shrnutí

Dokument popisuje návrh P2P bankovního uzlu, jeho architekturu, zpracování příkazů, ukládání dat a interní monitoring.
Slouží jako kompletní podklad pro implementaci a obhajobu školního projektu.

## 10. Doprovodná monitorovací webová aplikace (volitelné rozšíření)

Součástí návrhu je také **volitelné rozšíření** ve formě samostatné lokální webové aplikace,
která slouží k vizualizaci provozních dat bankovního uzlu.

Tato webová aplikace **není součástí core architektury bankovního uzlu** a nezasahuje do jeho
běhu ani aplikační logiky. Je navržena jako externí nástroj určený výhradně pro monitorování,
analýzu a prezentaci již uložených provozních snapshotů.

### 10.1 Účel webové aplikace

Cílem webové aplikace je:

- zobrazovat historická provozní data bankovního uzlu
- vizualizovat vývoj zátěže a stability systému v čase
- ověřit praktickou použitelnost návrhu provozních snapshotů
- zvýšit přehlednost a hodnotu monitorovacích dat

Webová aplikace slouží primárně pro vývojáře a pro účely obhajoby projektu.

### 10.2 Zdroj dat

Webová aplikace pracuje výhradně s již uloženými daty a:

- čte provozní snapshoty z databáze MySQL
- v případě nedostupnosti databáze může číst snapshoty ze souborového úložiště (JSON)
- pracuje pouze v režimu **read-only**

Webová aplikace nemá možnost:

- měnit stav bankovního uzlu
- zasahovat do aplikační logiky
- ovlivňovat síťovou komunikaci nebo persistence vrstvu

### 10.3 Oddělení od bankovního uzlu

Monitorovací webová aplikace:

- je spouštěna samostatně
- neběží ve stejném procesu jako bankovní uzel
- není vyžadována pro správnou funkci bankovního uzlu
- není součástí jeho runtime architektury

Z architektonického pohledu se jedná o **externí doprovodnou komponentu**,
která využívá výstupy bankovního uzlu, nikoliv jeho interní rozhraní.

### 10.4 Zařazení v architektuře

V architektonických diagramech bankovního uzlu není tato webová aplikace zobrazena
jako součást systému.

V případě kontextového diagramu může být znázorněna jako externí komponenta
komunikující nepřímo prostřednictvím databáze nebo souborového úložiště.

Tím je zachováno čisté oddělení mezi návrhem bankovního uzlu a jeho volitelným rozšířením.
