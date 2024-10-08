import { GridRenderEditCellParams, useGridApiContext } from "@mui/x-data-grid";
import { IconButton, Popover, Tooltip } from "@mui/material";
import { GridBaseColDef } from "@mui/x-data-grid/internals";
import { GridColDef } from "../adminGrid/adminGridInterfaces";
import PublicOutlinedIcon from "@mui/icons-material/PublicOutlined";
import { MouseEvent, useCallback, useContext, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { DataGridSpatialExtentPopoverContent } from "./dataGridSpatialExtentPopoverContent";
import { Coordinate } from "../../api/apiInterfaces";
import { PromptContext } from "../prompt/promptContext";

export const IsGridSpatialExtentColDef = (columnDef: GridColDef) =>
  columnDef.type === "custom" && columnDef.field === "coordinates";

export const TransformToSpatialExtentColumn = (columnDef: GridBaseColDef) => {
  columnDef.renderCell = () => (
    <IconButton size="small" color="inherit" disabled>
      <PublicOutlinedIcon fontSize="small" />
    </IconButton>
  );
  columnDef.renderEditCell = params => <DataGridSpatialExtentColumn params={params} />;
};

interface DataGridSpatialExtentColumnProps {
  params: GridRenderEditCellParams;
}

const DataGridSpatialExtentColumn = ({ params }: DataGridSpatialExtentColumnProps) => {
  const apiRef = useGridApiContext();
  const { t } = useTranslation();
  const [popoverAnchor, setPopoverAnchor] = useState<HTMLButtonElement | null>(null);
  const [spatialExtent, setSpatialExtent] = useState<Coordinate[]>(params.value);
  const { showPrompt } = useContext(PromptContext);

  const setDefaultSpatialExtent = useCallback(() => {
    if (params.value) {
      setSpatialExtent(params.value);
    } else {
      setSpatialExtent([
        { x: null, y: null },
        { x: null, y: null },
      ]);
    }
  }, [params.value]);

  useEffect(() => {
    setDefaultSpatialExtent();
  }, [setDefaultSpatialExtent]);

  return (
    <>
      <Tooltip title={t("spatialExtent")}>
        <IconButton
          sx={{ margin: "10px" }}
          size="small"
          color="inherit"
          onClick={(event: MouseEvent<HTMLButtonElement>) => {
            setPopoverAnchor(event.currentTarget);
          }}>
          <PublicOutlinedIcon fontSize="small" />
        </IconButton>
      </Tooltip>
      <Popover
        id={"spatial-extent-popover"}
        open={!!popoverAnchor}
        anchorEl={popoverAnchor}
        onClose={() => {
          const allNull = spatialExtent.every(coord => coord.x === null && coord.y === null);
          const noneNull = spatialExtent.every(coord => coord.x !== null && coord.y !== null);
          if (allNull || noneNull) {
            apiRef.current.setEditCellValue({
              id: params.id,
              field: "coordinates",
              value: spatialExtent,
            });
            setPopoverAnchor(null);
          } else {
            showPrompt(t("spatialExtentIncomplete"), [
              { label: t("cancel") },
              {
                label: t("resetCurrentChanges"),
                action: () => {
                  setDefaultSpatialExtent();
                  setPopoverAnchor(null);
                },
                color: "error",
                variant: "contained",
              },
            ]);
          }
        }}
        anchorOrigin={{
          vertical: "bottom",
          horizontal: "right",
        }}
        transformOrigin={{
          vertical: "top",
          horizontal: "right",
        }}>
        <DataGridSpatialExtentPopoverContent
          spatialExtent={spatialExtent}
          onChange={setSpatialExtent}
          reset={setDefaultSpatialExtent}
        />
      </Popover>
    </>
  );
};
