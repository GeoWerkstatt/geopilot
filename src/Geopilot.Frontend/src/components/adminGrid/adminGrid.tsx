import { FC, useEffect, useState } from "react";
import AddIcon from "@mui/icons-material/Add";
import {
  DataGrid,
  GridActionsCellItem,
  GridCellParams,
  GridPaginationModel,
  GridRowId,
  GridRowModes,
  GridRowModesModel,
} from "@mui/x-data-grid";
import { Tooltip } from "@mui/material";
import SaveOutlinedIcon from "@mui/icons-material/SaveOutlined";
import CancelOutlinedIcon from "@mui/icons-material/CancelOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import LinkOffIcon from "@mui/icons-material/LinkOff";
import { useTranslation } from "react-i18next";
import { AdminGridProps, DataRow, GridColDef } from "./adminGridInterfaces";
import {
  GridMultiSelectColDef,
  IsGridMultiSelectColDef,
  TransformToMultiSelectColumn,
} from "../dataGrid/dataGridMultiSelectColumn";
import { BaseButton } from "../buttons.tsx";

export const AdminGrid: FC<AdminGridProps> = ({ addLabel, data, columns, onSave, onDisconnect, disableRow }) => {
  const { t } = useTranslation();
  const [rows, setRows] = useState<DataRow[]>([]);
  const [rowModesModel, setRowModesModel] = useState<GridRowModesModel>({});
  const [editingRow, setEditingRow] = useState<GridRowId>();
  const [paginationModel, setPaginationModel] = useState({
    page: 0,
    pageSize: 10,
  });
  const defaultRow: DataRow = { id: 0 };

  useEffect(() => {
    if (data) {
      setRows(data);
      setRowModesModel({});
      setEditingRow(undefined);
    }
  }, [data]);

  const actionColumn: GridColDef = {
    field: "actions",
    type: "actions",
    headerName: "",
    flex: 0,
    resizable: false,
    cellClassName: "actions",
    getActions: ({ id }) => {
      if (id === disableRow) {
        return [];
      }

      const isInEditMode = rowModesModel[id]?.mode === GridRowModes.Edit;

      if (isInEditMode) {
        return [
          <Tooltip title={t("save")} key={`save-${id}`}>
            <GridActionsCellItem
              icon={<SaveOutlinedIcon />}
              label={t("save")}
              onClick={handleSaveClick(id)}
              color="inherit"
            />
          </Tooltip>,
          <Tooltip title={t("cancel")} key={`cancel-${id}`}>
            <GridActionsCellItem
              icon={<CancelOutlinedIcon />}
              label={t("cancel")}
              onClick={handleCancelClick(id)}
              color="inherit"
            />
          </Tooltip>,
        ];
      }

      return [
        <Tooltip title={t("edit")} key={`edit-${id}`}>
          <GridActionsCellItem
            icon={<EditOutlinedIcon />}
            label={t("edit")}
            onClick={handleEditClick(id)}
            color="inherit"
            disabled={editingRow !== undefined}
          />
        </Tooltip>,
        <Tooltip title={t("disconnect")} key={`disconnect-${id}`}>
          <GridActionsCellItem
            icon={<LinkOffIcon />}
            label={t("disconnect")}
            onClick={handleDisconnectClick(id)}
            color="error"
          />
        </Tooltip>,
      ];
    },
  };
  columns.forEach(column => {
    defaultRow[column.field] = undefined;
    if (IsGridMultiSelectColDef(column)) {
      TransformToMultiSelectColumn(column as GridMultiSelectColDef);
    }
  });
  const adminGridColumns: GridColDef[] = columns.concat(actionColumn);

  const addRow = () => {
    setEditingRow(defaultRow.id);
    setRows(oldRows => [...oldRows, defaultRow]);
  };

  useEffect(() => {
    if (editingRow === defaultRow.id) {
      const newPage = Math.ceil(rows.length / paginationModel.pageSize) - 1;
      setPaginationModel({ page: newPage, pageSize: paginationModel.pageSize });
      setRowModesModel(oldModel => ({
        ...oldModel,
        [defaultRow.id]: { mode: GridRowModes.Edit, fieldToFocus: columns[0].field },
      }));
    }
  }, [columns, defaultRow.id, editingRow, paginationModel.pageSize, rows]);

  const handleEditClick = (id: GridRowId) => () => {
    setRowModesModel({ ...rowModesModel, [id]: { mode: GridRowModes.Edit } });
    setEditingRow(id);
  };

  const handleSaveClick = (id: GridRowId) => () => {
    setRowModesModel({ ...rowModesModel, [id]: { mode: GridRowModes.View } });
    setEditingRow(undefined);
  };

  const handleDisconnectClick = (id: GridRowId) => () => {
    onDisconnect(rows.find(r => r.id === id) as DataRow);
  };

  const handleCancelClick = (id: GridRowId) => () => {
    setRowModesModel({
      ...rowModesModel,
      [id]: { mode: GridRowModes.View, ignoreModifications: true },
    });

    const editedRow = rows.find(row => row.id === id);
    if (editedRow?.id === 0) {
      setRows(rows.filter(row => row.id !== id));
    }
    setEditingRow(undefined);
  };

  const processRowUpdate = (updatedRow: DataRow) => {
    setRows(
      rows.map(row => {
        if (row.id === updatedRow.id) {
          if (row !== updatedRow) {
            onSave(updatedRow);
          }
          return updatedRow;
        } else {
          return row;
        }
      }),
    );
    return updatedRow;
  };

  const handleRowModesModelChange = (newRowModesModel: GridRowModesModel) => {
    if (editingRow !== undefined && newRowModesModel[editingRow]) {
      newRowModesModel[editingRow].mode = GridRowModes.Edit;
    }
    setRowModesModel(newRowModesModel);
  };

  const handlePaginationModelChange = (newPaginationModel: GridPaginationModel) => {
    setPaginationModel(newPaginationModel);
  };

  return (
    <>
      {!!addLabel && (
        <BaseButton
          variant="outlined"
          icon={<AddIcon />}
          sx={{ marginBottom: "20px" }}
          disabled={editingRow !== undefined}
          onClick={addRow}
          label={addLabel}
        />
      )}
      <DataGrid
        rows={rows}
        columns={adminGridColumns}
        editMode="row"
        isCellEditable={(params: GridCellParams) =>
          rowModesModel && rowModesModel[params.id]?.mode === GridRowModes.Edit
        }
        disableColumnSelector
        rowModesModel={rowModesModel}
        onRowModesModelChange={handleRowModesModelChange}
        processRowUpdate={processRowUpdate}
        hideFooterSelectedRowCount
        pagination
        paginationModel={paginationModel}
        onPaginationModelChange={handlePaginationModelChange}
        pageSizeOptions={[5, 10, 25]}
      />
    </>
  );
};
