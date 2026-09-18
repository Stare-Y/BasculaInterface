import { Paper, Typography, Box } from "@mui/material";

export default function ChartPlaceholder() {
  return (
    <Paper sx={{ height: 300, display: "flex", justifyContent: "center", alignItems: "center" }}>
      <Typography variant="h6" color="text.secondary">
        Aquí irá un gráfico
      </Typography>
    </Paper>
  );
}