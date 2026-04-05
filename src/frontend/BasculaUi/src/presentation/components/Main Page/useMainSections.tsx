
import { useMemo } from 'react';
import type { Section } from '../../../domain/Models/Section';

export const useMainSections = (): Section[] =>
  useMemo(() => [
    {
      index: '01',
      label: 'Órdenes',
      description: 'Gestiona las órdenes activas y su historial',
      icon: 'pi pi-list',
      path: '/Dashboard/Orders',
      colorKey: 'teal',
    },
    {
      index: '02',
      label: 'Dashboard',
      description: 'Vista general del sistema de medición',
      icon: 'pi pi-th-large',
      path: '/Dashboard',
      colorKey: 'blue',
    },
  ], []);