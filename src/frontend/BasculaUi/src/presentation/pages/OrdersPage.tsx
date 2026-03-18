// src/pages/OrdersPage.tsx
import { Box } from "@mui/material";
import  type {
  Column,
  FilterField
} from "../components/dashboard/ReutilisableTable";
import  ReutilisableTable from "../components/dashboard/ReutilisableTable";
import type { WeightEntry } from "../types/WeightEntry";
import { useEffect, useState } from "react";
import axios from "axios";
import { useNavigate } from "react-router-dom";



const columns: Column<WeightEntry>[] = [
  { id: "id", label: "ID", numeric: true },
  { id: "vehiclePlate", label: "Placas" },
  { id: "tareWeight", label: "Peso Tara (kg)", numeric: true },
  { id: "bruteWeight", label: "Peso Bruto (kg)", numeric: true },
  { id: "partnerId", label: "Proveedor", numeric: true },
  { id: "createdAt", label: "Fecha Creación" },
  { id: "concludeDate", label: "Fecha Finalización" },
  { id: "registeredBy", label: "Registrado por" }
];

const filterFields: FilterField<WeightEntry>[] = [
  { id: "vehiclePlate", label: "Placas", type: "text" },
  { id: "partnerId", label: "Proveedor", type: "number" },
  { id: "createdAt", label: "", type: "date" },
];

export default function OrdersPage() {
  const [rows, setRows] = useState<WeightEntry[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const navigate = useNavigate();
  
  useEffect(() => {
    const fetchWeights = async () => {
      try {
        const response = await axios.get<WeightEntry[]>(
          "http://localhost:6969/api/Weight/Pending"
        );

        setRows(response.data);
      } catch (error) {
        console.error("Error cargando pesos:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchWeights();
  }, []);

  return (
    <Box sx={{ p: 2 }}>
      <ReutilisableTable<WeightEntry>
        title="Entradas de Peso"
        columns={columns}
        filterFields={filterFields}
        rows={rows}
        onEditRow={(row) => navigate(`/dashboard/orders/${row.id}`)}
        onDeleteRow={(row) => console.log("Eliminar fila:", row)}
        onDeleteSelected={(ids) => console.log("Eliminar seleccionados:", ids)}
      />
    </Box>
  );
}