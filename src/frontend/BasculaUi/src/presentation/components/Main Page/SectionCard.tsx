
import React from 'react';
import { Button } from 'primereact/button';
import type { Section } from '../../../domain/Models/Section';
import styles from './MainPage.module.css';

interface Props {
  section: Section;
  onClick: () => void;
}

export const SectionCard = ({ section, onClick }: Props) => (
  <div
    className={`${styles.card} ${styles[`card_${section.colorKey}`]}`}
    onClick={onClick}
  >
    <div className={styles.cardAccent} />
    <div className={styles.cardIcon}>
      <i className={section.icon} />
    </div>
    <h3 className={styles.cardLabel}>{section.label}</h3>
    <p className={styles.cardDesc}>{section.description}</p>
    <Button
      label="Ir"
      icon="pi pi-arrow-right"
      iconPos="right"
      size="small"
      className={styles.cardBtn}
    />
    <span className={styles.cornerMark}>{section.index}</span>
  </div>
);