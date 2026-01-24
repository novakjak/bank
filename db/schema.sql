create database bank;
use bank;

create table accounts (
    account_number bigint not null,
    bank_code varchar(45) not null,
    balance bigint not null default 0,
    primary key (account_number, bank_code),
    check (balance >= 0)
);

create index idx_accounts_bank_account
    on accounts (bank_code, account_number);

create table snapshots (
    id bigint auto_increment primary key,
    created_at datetime not null default current_timestamp,
    uptime_seconds bigint not null,
    health_state enum('OK','DEGRADED','ERROR') not null,
    active_connections int not null,
    total_commands bigint not null,
    proxy_commands bigint not null,
    error_count bigint not null,
    persistence_strategy enum('MYSQL','CSV') not null
);

create index idx_snapshots_created_at
    on snapshots (created_at);

create table command_metrics (
    command_name varchar(32) primary key,
    execution_count bigint not null,
    error_count bigint not null,
    avg_execution_ms double not null
);

create table node_control (
    id int primary key check (id = 1),
    shutdown_requested boolean not null default false
);

insert into node_control (id, shutdown_requested)
values (1, false)
on duplicate key update shutdown_requested = false;
