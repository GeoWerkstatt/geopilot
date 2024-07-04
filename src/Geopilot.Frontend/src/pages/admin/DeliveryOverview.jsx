import { useContext, useEffect, useState } from "react";
import DeleteOutlinedIcon from "@mui/icons-material/DeleteOutlined";
import { useTranslation } from "react-i18next";
import { DataGrid } from "@mui/x-data-grid";
import { Button } from "@mui/material";
import { useAuth } from "../../auth";
import { PromptContext } from "../../components/prompt/promptContext.jsx";
import { AlertContext } from "../../components/alert/alertContext.jsx";

const useTranslatedColumns = t => {
  return [
    { field: "id", headerName: t("id"), width: 60 },
    {
      field: "date",
      headerName: t("deliveryDate"),
      valueFormatter: params => {
        const date = new Date(params.value);
        return `${date.toLocaleString()}`;
      },
      width: 180,
    },
    { field: "user", headerName: t("deliveredBy"), flex: 0.5, minWidth: 200 },
    { field: "mandate", headerName: t("mandate"), flex: 0.5, minWidth: 200 },
    { field: "comment", headerName: t("comment"), flex: 1, minWidth: 600 },
  ];
};

export const DeliveryOverview = () => {
  const { t } = useTranslation();
  const columns = useTranslatedColumns(t);
  const [deliveries, setDeliveries] = useState(undefined);
  const [selectedRows, setSelectedRows] = useState([]);
  const [alertMessages, setAlertMessages] = useState([]);
  const [currentAlert, setCurrentAlert] = useState(undefined);
  const { showPrompt } = useContext(PromptContext);
  const { showAlert, alertIsOpen } = useContext(AlertContext);

  const { user } = useAuth();

  if (user && deliveries === undefined) {
    loadDeliveries();
  }

  useEffect(() => {
    if (alertMessages.length && (!currentAlert || !alertIsOpen)) {
      setCurrentAlert(alertMessages[0]);
      setAlertMessages(prev => prev.slice(1));
      showAlert(alertMessages[0], "error");
    }
  }, [alertMessages, currentAlert, alertIsOpen, showAlert]);

  async function loadDeliveries() {
    try {
      const response = await fetch("/api/v1/delivery");
      if (response.status === 200) {
        const deliveries = await response.json();
        setDeliveries(
          deliveries.map(d => ({
            id: d.id,
            date: d.date,
            user: d.declaringUser.fullName,
            mandate: d.mandate.name,
            comment: d.comment,
          })),
        );
      }
    } catch (error) {
      setAlertMessages(prev => [...prev, t("deliveryOverviewLoadingError", { error: error })]);
    }
  }

  async function handleDelete() {
    for (const row of selectedRows) {
      try {
        const response = await fetch("api/v1/delivery/" + row, {
          method: "DELETE",
        });
        if (response.status === 404) {
          setAlertMessages(prev => [...prev, t("deliveryOverviewDeleteIdNotExistError", { id: row })]);
        } else if (response.status === 500) {
          setAlertMessages(prev => [...prev, t("deliveryOverviewDeleteIdError", { id: row })]);
        }
      } catch (error) {
        setAlertMessages(prev => [...prev, t("deliveryOverviewDeleteError", { error: error })]);
      }
    }
    await loadDeliveries();
  }

  return (
    <>
      {deliveries?.length > 0 && (
        <DataGrid
          sx={{
            fontFamily: "system-ui, -apple-syste",
          }}
          pagination
          rows={deliveries}
          columns={columns}
          initialState={{
            pagination: {
              paginationModel: { page: 0, pageSize: 5 },
            },
          }}
          pageSizeOptions={[5, 10, 25]}
          checkboxSelection
          onRowSelectionModelChange={newSelection => {
            setSelectedRows(newSelection);
          }}
          hideFooterRowCount
          hideFooterSelectedRowCount
        />
      )}
      {selectedRows.length > 0 && (
        <div className="center-button-container">
          <Button
            color="error"
            variant="contained"
            startIcon={<DeleteOutlinedIcon />}
            onClick={() => {
              showPrompt(t("deleteDeliveryConfirmationTitle"), t("deleteDeliveryConfirmationMessage"), [
                { label: t("cancel"), action: null },
                { label: t("delete"), action: handleDelete, color: "error", variant: "contained" },
              ]);
            }}>
            <div>{t("deleteDelivery", { count: selectedRows.length })}</div>
          </Button>
        </div>
      )}
    </>
  );
};

export default DeliveryOverview;
