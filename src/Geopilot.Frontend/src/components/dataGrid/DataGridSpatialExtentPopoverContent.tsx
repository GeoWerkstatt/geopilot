import { ChangeEvent, FC } from "react";
import { Coordinate } from "../../AppInterfaces.ts";
import { useTranslation } from "react-i18next";
import { Box, Button, TextField } from "@mui/material";

interface SpatialExtentPopoverContentProps {
  spatialExtent: Coordinate[];
  onChange: (spatialExtent: Coordinate[]) => void;
  reset: () => void;
}

export const DataGridSpatialExtentPopoverContent: FC<SpatialExtentPopoverContentProps> = ({
  spatialExtent,
  onChange,
  reset,
}) => {
  const { t } = useTranslation();

  const handleChange = (index: number, type: "x" | "y") => (e: ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.value === "" ? null : +e.target.value;
    onChange(spatialExtent.map((coord, i) => (i === index ? { ...coord, [type]: newValue } : coord)));
  };

  const renderCoordinateRow = (index: number) => (
    <div className="spatial-extent-coordinate-row">
      <Box
        sx={{
          margin: "10px 20px 10px 10px",
          width: "20px",
          height: "20px",
          border: `1px solid black`,
          borderTopWidth: index === 0 ? 0 : undefined,
          borderRightWidth: index === 0 ? 0 : undefined,
          borderLeftWidth: index === 1 ? 0 : undefined,
          borderBottomWidth: index === 1 ? 0 : undefined,
        }}
      />
      <TextField
        type="number"
        size="small"
        sx={{ marginRight: "20px" }}
        label={t("longitude")}
        value={spatialExtent[index]?.x ?? ""}
        onChange={handleChange(index, "x")}
      />
      <TextField
        type="number"
        size="small"
        label={t("latitude")}
        value={spatialExtent[index]?.y ?? ""}
        onChange={handleChange(index, "y")}
      />
    </div>
  );

  return (
    <div className="spatial-extent-popover">
      <h6>{t("spatialExtent")}</h6>
      {renderCoordinateRow(0)}
      {renderCoordinateRow(1)}
      <div style={{ display: "flex", justifyContent: "flex-end" }}>
        <Button size="small" onClick={reset}>
          {t("reset")}
        </Button>
      </div>
    </div>
  );
};
