-- =============================================================
-- Информационная система подбора кандидатов на вакантные должности
-- Создание структуры базы данных (PostgreSQL)
-- =============================================================

-- Таблица «Пользователи»
CREATE TABLE IF NOT EXISTS users (
    id            SERIAL       PRIMARY KEY,
    login         VARCHAR(50)  UNIQUE NOT NULL,
    phone         VARCHAR(20)  UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    fio           VARCHAR(150) NOT NULL,
    role          VARCHAR(30)  NOT NULL,
    email         VARCHAR(100) UNIQUE,
    created_at    TIMESTAMP    NOT NULL DEFAULT NOW(),
    is_active     BOOLEAN      NOT NULL DEFAULT TRUE
);

-- Таблица «Компании»
CREATE TABLE IF NOT EXISTS companies (
    id       SERIAL       PRIMARY KEY,
    name     VARCHAR(150) NOT NULL,
    industry VARCHAR(100),
    city     VARCHAR(100),
    phone    VARCHAR(30),
    email    VARCHAR(100),
    website  VARCHAR(150)
);

-- Таблица «Вакансии»
CREATE TABLE IF NOT EXISTS vacancies (
    id                  SERIAL       PRIMARY KEY,
    company_id          INTEGER      NOT NULL REFERENCES companies (id) ON DELETE CASCADE,
    title               VARCHAR(150) NOT NULL,
    description         TEXT,
    salary_min          INTEGER,
    salary_max          INTEGER,
    work_format         VARCHAR(20),
    required_experience VARCHAR(30),
    required_age_min    INTEGER,
    required_age_max    INTEGER,
    required_gender     VARCHAR(10),
    status              VARCHAR(30)  NOT NULL DEFAULT 'active',
    created_at          TIMESTAMP    NOT NULL DEFAULT NOW()
);

-- Таблица «Кандидаты»
CREATE TABLE IF NOT EXISTS candidates (
    id              SERIAL       PRIMARY KEY,
    fio             VARCHAR(150) NOT NULL,
    age             INTEGER,
    gender          VARCHAR(10),
    phone           VARCHAR(30),
    email           VARCHAR(100),
    city            VARCHAR(100),
    pc_level        VARCHAR(30),
    language        VARCHAR(50),
    team_management VARCHAR(20),
    work_format     VARCHAR(20),
    experience      VARCHAR(30),
    salary          INTEGER,
    created_at      TIMESTAMP    NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMP    NOT NULL DEFAULT NOW()
);

-- Таблица «Навыки»
CREATE TABLE IF NOT EXISTS skills (
    id   SERIAL       PRIMARY KEY,
    name VARCHAR(100) UNIQUE NOT NULL
);

-- Таблица «Связь кандидат–навык» (отношение «многие ко многим»)
CREATE TABLE IF NOT EXISTS candidate_skills (
    candidate_id INTEGER NOT NULL REFERENCES candidates (id) ON DELETE CASCADE,
    skill_id     INTEGER NOT NULL REFERENCES skills (id) ON DELETE CASCADE,
    PRIMARY KEY (candidate_id, skill_id)
);

-- Таблица «Связь вакансия–навык» (требуемые навыки вакансии)
CREATE TABLE IF NOT EXISTS vacancy_skills (
    vacancy_id INTEGER NOT NULL REFERENCES vacancies (id) ON DELETE CASCADE,
    skill_id   INTEGER NOT NULL REFERENCES skills (id) ON DELETE CASCADE,
    PRIMARY KEY (vacancy_id, skill_id)
);

-- Таблица «Образование»
CREATE TABLE IF NOT EXISTS education (
    id           SERIAL  PRIMARY KEY,
    candidate_id INTEGER NOT NULL REFERENCES candidates (id) ON DELETE CASCADE,
    institution  VARCHAR(150),
    specialty    VARCHAR(150),
    degree       VARCHAR(50),
    year_end     INTEGER
);

-- Таблица «Отклики»
CREATE TABLE IF NOT EXISTS applications (
    id           SERIAL      PRIMARY KEY,
    candidate_id INTEGER     NOT NULL REFERENCES candidates (id) ON DELETE CASCADE,
    vacancy_id   INTEGER     NOT NULL REFERENCES vacancies (id) ON DELETE CASCADE,
    status       VARCHAR(30) NOT NULL DEFAULT 'new',
    applied_at   TIMESTAMP   NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMP   NOT NULL DEFAULT NOW(),
    UNIQUE (candidate_id, vacancy_id)
);

-- Таблица «Уведомления»
CREATE TABLE IF NOT EXISTS notifications (
    id         SERIAL       PRIMARY KEY,
    user_id    INTEGER      NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    title      VARCHAR(150) NOT NULL,
    message    TEXT         NOT NULL,
    type       VARCHAR(30),
    is_read    BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP    NOT NULL DEFAULT NOW()
);

-- Таблица «Журнал действий»
CREATE TABLE IF NOT EXISTS action_log (
    id         SERIAL       PRIMARY KEY,
    user_id    INTEGER      REFERENCES users (id) ON DELETE SET NULL,
    action     VARCHAR(100) NOT NULL,
    entity     VARCHAR(50),
    entity_id  INTEGER,
    created_at TIMESTAMP    NOT NULL DEFAULT NOW()
);

-- Регистрация выполняется по логину, поэтому телефон и email необязательны.
-- Команды нужны для баз, созданных прежней версией скрипта.
ALTER TABLE users ALTER COLUMN phone DROP NOT NULL;
ALTER TABLE users ALTER COLUMN email DROP NOT NULL;

-- Индексы для ускорения поиска и фильтрации
CREATE INDEX IF NOT EXISTS ix_candidates_city    ON candidates (city);
CREATE INDEX IF NOT EXISTS ix_candidates_salary  ON candidates (salary);
CREATE INDEX IF NOT EXISTS ix_vacancies_company  ON vacancies (company_id);
CREATE INDEX IF NOT EXISTS ix_applications_vac   ON applications (vacancy_id);
CREATE INDEX IF NOT EXISTS ix_applications_cand  ON applications (candidate_id);
CREATE INDEX IF NOT EXISTS ix_notifications_user ON notifications (user_id, is_read);
CREATE INDEX IF NOT EXISTS ix_action_log_user    ON action_log (user_id);
