import { AppBar, Toolbar, Typography } from "@mui/material";

export default function Topbar() {
  return (
    <AppBar position="fixed" sx={{ zIndex: 1300 }}>
      <Toolbar>
        <Typography variant="h6">Bascula xd</Typography>
      </Toolbar>
    </AppBar>
  );
}