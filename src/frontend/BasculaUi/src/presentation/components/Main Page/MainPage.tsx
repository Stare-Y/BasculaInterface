import React from 'react';
import { useNavigate } from 'react-router-dom';
import { Card } from 'primereact/card';
import { Button } from 'primereact/button';

const sections = [
  {
    label: 'Órdenes',
    description: 'Gestiona las órdenes activas y su historial',
    icon: 'pi pi-list',
    path: '/Dashboard/Orders',
    color: '#4f46e5',
  },
  {
    label: 'Dashboard',
    description: 'Vista general del sistema',
    icon: 'pi pi-th-large',
    path: '/Dashboard',
    color: '#0ea5e9',
  },
  // Agrega más secciones aquí
];

const MainPage = () => {
  const navigate = useNavigate();

  return (
    <div style={{
      minHeight: '100vh',
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      padding: '2rem',
      background: 'var(--surface-ground)',
    }}>
      <h1 style={{ marginBottom: '0.5rem' }}>Sistema de Básculas</h1>
      <p style={{ color: 'var(--text-color-secondary)', marginBottom: '2.5rem' }}>
        Selecciona una sección para comenzar
      </p>

      <div style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
        gap: '1.5rem',
        width: '100%',
        maxWidth: '800px',
      }}>
        {sections.map((section) => (
          <Card
            key={section.path}
            style={{ cursor: 'pointer', textAlign: 'center' }}
            onClick={() => navigate(section.path)}
          >
            <i
              className={section.icon}
              style={{ fontSize: '2.5rem', color: section.color, marginBottom: '1rem', display: 'block' }}
            />
            <h3 style={{ margin: '0 0 0.5rem' }}>{section.label}</h3>
            <p style={{ color: 'var(--text-color-secondary)', fontSize: '0.875rem', margin: '0 0 1rem' }}>
              {section.description}
            </p>
            <Button label="Ir" icon="pi pi-arrow-right" iconPos="right" size="small" />
          </Card>
        ))}
      </div>
    </div>
  );
};

export default MainPage;