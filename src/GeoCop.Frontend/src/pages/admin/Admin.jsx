import * as React from "react";
import { useState } from "react";
import {
    AuthenticatedTemplate,
    UnauthenticatedTemplate,
    useMsal,
} from "@azure/msal-react";
import { Button, Modal } from "react-bootstrap";
import { GoTrash } from "react-icons/go";
import { DataGrid, deDE } from "@mui/x-data-grid";
import styled from "styled-components";

const IconButton = styled(Button)`
  display: flex;
  align-items: center;
`;

const CenterButtonContainer = styled("div")`
  display: flex;
  justify-content: center;
`;

const columns = [
    { field: "id", headerName: "ID", width: 60 },
    { field: "date", headerName: "Abgabedatum", flex: 1 },
    { field: "declaringUser", headerName: "Abgegeben von", flex: 1 },
    { field: "deliveryMandate", headerName: "Operat", flex: 1 },
];

export const Admin = ({ clientSettings }) => {
    const [deliveries, setDeliveries] = useState([]);
    const [selectedRows, setSelectedRows] = useState([]);
    const [showModal, setShowModal] = useState(false);

    const { instance } = useMsal();
    const activeAccount = instance.getActiveAccount();

    if (activeAccount && deliveries.length == 0) {
        fetch("/api/v1/delivery")
            .then(
                (res) =>
                    res.headers.get("content-type")?.includes("application/json") &&
                    res.json(),
            )
            .then((deliveries) => {
                setDeliveries(deliveries);
            });
    }

    async function handleDelete() {
        setShowModal(false);
        fetch("/api/v1/delivery", {
            method: "DELETE",
            body: JSON.stringify(selectedRows),
        }).then((deliveries) => {
            setDeliveries(deliveries);
            setSelectedRows([]);
        });
    }

    return (
        <>
            <main>
                <UnauthenticatedTemplate>
                    <div className="admin-no-access">
                        <div className="app-subtitle">Bitte melden Sie sich an.</div>
                    </div>
                </UnauthenticatedTemplate>
                <AuthenticatedTemplate>
                    <div className="app-title">Datenabgaben</div>
                    <DataGrid
                        localeText={deDE.components.MuiDataGrid.defaultProps.localeText}
                        sx={{
                            margin: "20px 35px",
                            fontFamily: "system-ui, -apple-syste",
                        }}
                        pagination
                        rows={deliveries}
                        columns={columns}
                        initialState={{
                            pagination: {
                                paginationModel: { page: 0, pageSize: 10 },
                            },
                        }}
                        pageSizeOptions={[5, 10, 25]}
                        checkboxSelection
                        onRowSelectionModelChange={(newSelection) => {
                            setSelectedRows(newSelection);
                        }}
                        hideFooterRowCount
                        hideFooterSelectedRowCount
                    />
                    {selectedRows.length > 0 && (
                        <CenterButtonContainer>
                            <IconButton
                                onClick={() => {
                                    setShowModal(true);
                                }}
                            >
                                <GoTrash />
                                <div style={{ marginLeft: 10 }}>
                                    {selectedRows.length} Datenabgabe
                                    {selectedRows.length > 1 ? "n" : ""} löschen
                                </div>
                            </IconButton>
                        </CenterButtonContainer>
                    )}
                    <Modal show={showModal} animation={false}>
                        <Modal.Body>
                            Möchten Sie die Datenabgabe wirklich löschen? Diese Aktion kann
                            nicht rückgängig gemacht werden.
                        </Modal.Body>
                        <Modal.Footer>
                            <Button
                                variant="secondary"
                                onClick={() => {
                                    setShowModal(false);
                                }}
                            >
                                Abbrechen
                            </Button>
                            <Button variant="danger" onClick={handleDelete}>
                                Löschen
                            </Button>
                        </Modal.Footer>
                    </Modal>
                </AuthenticatedTemplate>
            </main>
        </>
    );
};

export default Admin;