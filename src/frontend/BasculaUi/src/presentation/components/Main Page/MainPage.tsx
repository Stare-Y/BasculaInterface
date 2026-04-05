// src/presentation/pages/MainPage/MainPage.tsx
import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useMainSections } from './useMainSections';
import { SectionCard } from './SectionCard';
import styles from './MainPage.module.css';

const MainPage = () => {
  const navigate = useNavigate();
  const sections = useMainSections();

  return (
    <div className={styles.page}>
      <div className={styles.gridBg} />
      <div className={styles.accentLine} />

      <header className={styles.header}>
        <div className={styles.badge}>
          <span className={styles.dot} />
          Sistema Activo
        </div>
        <h1 className={styles.title}>
          Sistema de <span>Básculas</span>
        </h1>
        <p className={styles.subtitle}>Selecciona una sección para comenzar</p>
      </header>

      <div className={styles.grid}>
        {sections.map((section) => (
          <SectionCard
            key={section.path}
            section={section}
            onClick={() => navigate(section.path)}
          />
        ))}
      </div>

      <footer className={styles.footer}>
        <span className={styles.footerText}>Báscula Interface v1.0</span>
        <div className={styles.statusBar}>
          <span className={styles.statusDot} />
          Sistema en línea
        </div>
      </footer>
    </div>
  );
};

export default MainPage;