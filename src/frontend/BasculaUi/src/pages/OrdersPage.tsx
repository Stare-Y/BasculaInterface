// src/pages/OrdersPage.tsx
import { Box } from "@mui/material";
import  type {
  Column,
  FilterField
} from "../components/dashboard/ReutilisableTable";
import  ReutilisableTable from "../components/dashboard/ReutilisableTable";
export interface Dessert {
  id: number;
  name: string;
  calories: number;
  fat: number;
  carbs: number;
  protein: number;
}

const columns: Column<Dessert>[] = [
  { id: "name", label: "Nombre", disablePadding: true },
  { id: "calories", label: "Calorías", numeric: true },
  { id: "fat", label: "Grasa (g)", numeric: true },
  { id: "carbs", label: "Carbs (g)", numeric: true },
  { id: "protein", label: "Proteína (g)", numeric: true }
];

const filterFields: FilterField<Dessert>[] = [
  { id: "name", label: "Nombre", type: "text" },
  { id: "calories", label: "Calorías exactas", type: "number" }
];

export default function OrdersPage() {
  return (
    <Box sx={{ p: 2 }}>
      <ReutilisableTable<Dessert>
        title="Postres"
        columns={columns}
        filterFields={filterFields}
        serverSide
        // endpoint="/api/desserts"
        onEditRow={(row) => console.log("Editar fila:", row)}
        onDeleteRow={(row) => console.log("Eliminar fila:", row)}
        onDeleteSelected={(ids) => console.log("Eliminar seleccionados:", ids)}
      />
    </Box>
  );
}