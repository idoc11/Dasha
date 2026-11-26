-- База данных программы SEO_LoadSimulator (PostgreSQL 14+)
-- Выполнить: createdb simdb && psql -d simdb -f sql/sim_schema.sql -f sql/sim_seed.sql
drop table if exists sim_run, scenario, app_user, role cascade;

create table role (
  role_id   serial primary key,
  role_name varchar(30) not null unique
);
create table app_user (
  user_id       serial primary key,
  login         varchar(30)  not null unique,
  password_hash varchar(200) not null,
  full_name     varchar(100) not null,
  role_id       int not null references role(role_id),
  is_active     boolean not null default true
);
create table scenario (
  scenario_id   serial primary key,
  user_id       int not null references app_user(user_id) on delete cascade,
  name          varchar(80) not null,
  per_day       int          not null check (per_day between 10 and 20000),
  peak_k        numeric(4,2) not null check (peak_k between 1 and 5),
  service_min   numeric(5,1) not null check (service_min between 1 and 60),
  windows       int          not null check (windows between 1 and 100),
  open_hour     int          not null check (open_hour between 0 and 22),
  close_hour    int          not null check (close_hour between 1 and 24),
  priv_pct      numeric(5,1) not null check (priv_pct between 0 and 100),
  appt_pct      numeric(5,1) not null check (appt_pct between 0 and 100),
  noshow_pct    numeric(5,1) not null check (noshow_pct between 0 and 50),
  norm_min      numeric(5,1) not null check (norm_min between 1 and 120),
  dist          smallint     not null check (dist between 0 and 2),
  seed          int          not null,
  stress        boolean      not null,
  runs          int          not null check (runs between 1 and 1000),
  created_at    timestamp    not null default now(),
  unique (user_id, name)
);
create table sim_run (
  run_id        bigserial primary key,
  user_id       int not null references app_user(user_id),
  run_at        timestamp not null default now(),
  per_day       int          not null,
  peak_k        numeric(4,2) not null,
  service_min   numeric(5,1) not null,
  windows       int          not null,
  open_hour     int          not null,
  close_hour    int          not null,
  priv_pct      numeric(5,1) not null,
  appt_pct      numeric(5,1) not null,
  noshow_pct    numeric(5,1) not null,
  norm_min      numeric(5,1) not null,
  dist          smallint     not null,
  seed          int          not null,
  stress        boolean      not null,
  runs          int          not null,
  ok_share      numeric(5,1) not null,
  mean_wait     numeric(7,1) not null,
  p95_wait      numeric(7,1) not null,
  days_ok_pct   numeric(5,1) not null,
  verdict       boolean      not null
);
create index ix_run_user on sim_run(user_id, run_at desc);
