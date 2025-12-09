import React from "react";
import {
  alpha,
  Box,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TableSortLabel,
  Toolbar,
  Typography,
  Paper,
  Checkbox,
  IconButton,
  Tooltip,
  Stack,
  TextField,
  Button,
  LinearProgress
} from "@mui/material";
import DeleteIcon from "@mui/icons-material/Delete";
import EditSquareIcon from '@mui/icons-material/EditSquare';

import FilterListIcon from "@mui/icons-material/FilterList";
import { visuallyHidden } from "@mui/utils";
export type Order = "asc" | "desc";

export interface Column<T> {
  id: keyof T;
  label: string;
  numeric?: boolean;
  disablePadding?: boolean;
}

export interface FilterField<T> {
  id: keyof T;
  label: string;
  type?: "text" | "number"| "date";
}

interface ReutilizableTableProps<T extends { id: any }> {
  title?: string;
  rows?: T[];
  columns: Column<T>[];
  filterFields?: FilterField<T>[];
  onEditRow?: (row: T) => void;
  onDeleteRow?: (row: T) => void;
  onDeleteSelected?: (ids: any[]) => void;
}



export default function ReutilizableTable<T extends { id: any }>({
  title = "Tabla",
  rows = [],
  columns,
  filterFields = [],
  onEditRow,
  onDeleteRow,
  onDeleteSelected
}: ReutilizableTableProps<T>) {
  const [order, setOrder] = React.useState<Order>("asc");
  const [orderBy, setOrderBy] = React.useState<keyof T | "">("");
  const [selected, setSelected] = React.useState<readonly any[]>([]);
  const [page, setPage] = React.useState(0);
  const [dense, setDense] = React.useState(false);
  const [rowsPerPage, setRowsPerPage] = React.useState(5);
  const [search, setSearch] = React.useState("");
  const [filters, setFilters] = React.useState<Record<string, string>>({});
  const [loading, setLoading] = React.useState(false);

  const [internalRows, setInternalRows] = React.useState<T[]>(rows);
  const [totalRows, setTotalRows] = React.useState(rows.length);

  // Sync para modo client-side si cambian las rows props


  const handleRequestSort = (_: unknown, property: keyof T) => {
    const isAsc = orderBy === property && order === "asc";
    setOrder(isAsc ? "desc" : "asc");
    setOrderBy(property);
   
  };

  const handleSelectAllClick = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.checked) {
      const newSelected = internalRows.map((n) => n.id);
      setSelected(newSelected);
    } else {
      setSelected([]);
    }
  };

  const handleRowClick = (_: unknown, id: any) => {
    const selectedIndex = selected.indexOf(id);
    let newSelected: readonly any[] = [];

    if (selectedIndex === -1) newSelected = [...selected, id];
    else newSelected = selected.filter((x) => x !== id);

    setSelected(newSelected);
  };

  const isSelected = (id: any) => selected.indexOf(id) !== -1;

  const handleChangePage = (_: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const handleChangeDense = (event: React.ChangeEvent<HTMLInputElement>) => {
    setDense(event.target.checked);
  };

  // --------- SERVER SIDE FETCH (axios) ----------
 


  // ---------- CLIENT SIDE PROCESSING ----------
  const processedRows = React.useMemo(() => {
    let temp = [...rows];

    // Búsqueda simple: busca en todas las columnas string
    if (search.trim() !== "") {
      const s = search.toLowerCase();
      temp = temp.filter((row) =>
        columns.some((col) => {
          const value = row[col.id];
          return (
            typeof value === "string" &&
            value.toString().toLowerCase().includes(s)
          );
        })
      );
    }

    // Filtros por campo
    Object.entries(filters).forEach(([key, value]) => {
      if (!value) return;
      temp = temp.filter((row) => {
        const cell = row[key as keyof T];
        if (typeof cell === "number") {
          return cell === Number(value);
        }
        return cell?.toString().toLowerCase().includes(value.toLowerCase());
      });
    });

    // Sort
    if (orderBy) {
      temp.sort((a, b) => {
        const aValue = a[orderBy];
        const bValue = b[orderBy];
        if (aValue == null || bValue == null) return 0;
        if (bValue < aValue) return order === "desc" ? -1 : 1;
        if (bValue > aValue) return order === "desc" ? 1 : -1;
        return 0;
      });
    }

    setTotalRows(temp.length);
    return temp;
  }, [rows, columns, search, filters, orderBy, order,  internalRows]);

  const visibleRows = React.useMemo(() => {
    return processedRows.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage);
  }, [processedRows, page, rowsPerPage,  internalRows]);

  // ---------- HANDLERS UI ----------
  const handleFilterChange = (fieldId: string, value: string) => {
    setFilters((prev) => ({
      ...prev,
      [fieldId]: value
    }));
  };

  const handleSearchChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setSearch(event.target.value);
   
  };

  const handleExportCSV = () => {
    const rowsToExport = processedRows;

    if (!rowsToExport.length) return;

    const header = columns.map((c) => c.label).join(",");
    const dataLines = rowsToExport.map((row) =>
      columns
        .map((c) => {
          const val = row[c.id];
          const clean = val == null ? "" : String(val).replace(/"/g, '""');
          return `"${clean}"`;
        })
        .join(",")
    );

    const csvContent = [header, ...dataLines].join("\n");
    const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
    const url = window.URL.createObjectURL(blob);

    const link = document.createElement("a");
    link.href = url;
    link.setAttribute("download", `${title.replace(/\s+/g, "_").toLowerCase()}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const handleDeleteSelectedClick = () => {
    if (onDeleteSelected) {
      onDeleteSelected(selected);
    }
  };

  // ---------- RENDER ----------
  const numSelected = selected.length;

  return (
    <Box sx={{ width: "100%" }}>
      <Paper sx={{ width: "100%", mb: 2 }}>
        {loading && <LinearProgress />}

        {/* TOOLBAR */}
        <Toolbar
          sx={[
            {
              pl: 2,
              pr: 1,
              gap: 2,
              flexWrap: "wrap"
            },
            numSelected > 0 && {
              bgcolor: (theme) =>
                alpha(
                  theme.palette.primary.main,
                  theme.palette.action.activatedOpacity
                )
            }
          ]}
        >
          {numSelected > 0 ? (
            <Typography sx={{ flex: "1 1 100%" }} color="inherit" variant="subtitle1">
              {numSelected} seleccionados
            </Typography>
          ) : (
            <Typography sx={{ flex: "1 1 100%" }} variant="h6">
              {title}
            </Typography>
          )}

          {numSelected > 0 ? (
            <Tooltip title="Eliminar seleccionados">
              <IconButton onClick={handleDeleteSelectedClick}>
                <DeleteIcon />
              </IconButton>
            </Tooltip>
          ) : (
            <Stack direction="row" spacing={1} alignItems="center">
              <TextField
                size="small"
                label="Buscar"
                value={search}
                onChange={handleSearchChange}
              />
              <Tooltip title="Filtros">
                <IconButton>
                  <FilterListIcon />
                </IconButton>
              </Tooltip>
              <Button variant="outlined" onClick={handleExportCSV}>
                Exportar CSV
              </Button>
            </Stack>
          )}
        </Toolbar>

        {/* FILTROS */}
        {filterFields.length > 0 && (
          <Box sx={{ px: 2, pb: 2 }}>
            <Stack direction="row" spacing={2} flexWrap="wrap">
              {filterFields.map((f) => (
                <TextField
                  key={String(f.id)}
                  label={f.label}
                  size="small"
                  type={f.type || "text"}
                  value={filters[String(f.id)] || ""}
                  onChange={(e) => handleFilterChange(String(f.id), e.target.value)}
                />
              ))}
            </Stack>
          </Box>
        )}

        {/* TABLA */}
        <TableContainer>
          <Table sx={{ minWidth: 750 }} size={dense ? "small" : "medium"}>
            <TableHead>
              <TableRow>
                <TableCell padding="checkbox">
                  <Checkbox
                    color="primary"
                    indeterminate={
                      numSelected > 0 && numSelected < internalRows?.length
                    }
                    checked={
                      internalRows?.length > 0 && numSelected === internalRows?.length
                    }
                    onChange={handleSelectAllClick}
                  />
                </TableCell>
                {columns.map((col) => (
                  <TableCell
                    key={String(col.id)}
                    align={col.numeric ? "right" : "left"}
                    padding={col.disablePadding ? "none" : "normal"}
                    sortDirection={orderBy === col.id ? order : false}
                  >
                    <TableSortLabel
                      active={orderBy === col.id}
                      direction={orderBy === col.id ? order : "asc"}
                      onClick={(e) => handleRequestSort(e, col.id)}
                    >
                      {col.label}
                      {orderBy === col.id && (
                        <Box component="span" sx={visuallyHidden}>
                          {order === "desc"
                            ? "sorted descending"
                            : "sorted ascending"}
                        </Box>
                      )}
                    </TableSortLabel>
                  </TableCell>
                ))}
                <TableCell align="right">Acciones</TableCell>
              </TableRow>
            </TableHead>

            <TableBody>
              {visibleRows?.map((row, index) => {
                const isItemSelected = isSelected(row.id);
                const labelId = `enhanced-table-checkbox-${index}`;

                return (
                  <TableRow
                    hover
                    role="checkbox"
                    aria-checked={isItemSelected}
                    tabIndex={-1}
                    key={row.id}
                    selected={isItemSelected}
                    sx={{ cursor: "pointer" }}
                    onClick={(e) => handleRowClick(e, row.id)}
                  >
                    <TableCell padding="checkbox">
                      <Checkbox
                        color="primary"
                        checked={isItemSelected}
                        inputProps={{ "aria-labelledby": labelId }}
                      />
                    </TableCell>

                    {columns.map((col) => (
                      <TableCell
                        key={String(col.id)}
                        align={col.numeric ? "right" : "left"}
                        padding={col.disablePadding ? "none" : "normal"}
                      >
                        {String(row[col.id] ?? "")}
                      </TableCell>
                    ))}

                    {/* ACCIONES */}
                    <TableCell align="right" onClick={(e) => e.stopPropagation()}>
                      {onEditRow && (
                        <IconButton size="small" onClick={() => onEditRow(row)}>
                          <EditSquareIcon fontSize="small" />
                        </IconButton>
                      )}
                      {onDeleteRow && (
                        <IconButton size="small" onClick={() => onDeleteRow(row)}>
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      )}
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </TableContainer>

        {/* PAGINACIÓN */}
        <TablePagination
          rowsPerPageOptions={[5, 10, 25]}
          component="div"
          count={totalRows}
          rowsPerPage={rowsPerPage}
          page={page}
          onPageChange={handleChangePage}
          onRowsPerPageChange={handleChangeRowsPerPage}
        />
      </Paper>

      {/* DENSIDAD */}

    </Box>
  );
}