﻿import { useAuth } from "./auth";
import { useTranslation } from "react-i18next";
import * as React from "react";
import {
  AppBar,
  Box,
  Button,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  Toolbar,
  Typography,
} from "@mui/material";
import MenuIcon from "@mui/icons-material/Menu";
import AccountCircleOutlinedIcon from "@mui/icons-material/AccountCircleOutlined";
import { LoggedInTemplate } from "./auth/LoggedInTemplate.jsx";
import { LoggedOutTemplate } from "./auth/LoggedOutTemplate.jsx";
import { AdminTemplate } from "./auth/AdminTemplate.jsx";
import { useLocation, useNavigate } from "react-router-dom";

export const Header = ({ clientSettings, hasDrawerToggle, handleDrawerToggle }) => {
  const { user, login, logout } = useAuth();
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const [userMenuOpen, setUserMenuOpen] = React.useState(false);

  const toggleUserMenu = newOpen => () => {
    setUserMenuOpen(newOpen);
  };

  return (
    <>
      <AppBar>
        <Toolbar
          sx={{
            display: "flex",
            flexDirection: "row",
            justifyContent: "space-between",
          }}>
          <Box sx={{ display: "flex", flexDirection: "row", alignItems: "center" }}>
            {hasDrawerToggle ? (
              <>
                <IconButton
                  color="inherit"
                  aria-label="open drawer"
                  edge="start"
                  onClick={handleDrawerToggle}
                  sx={{ mr: 2, display: { sm: "none" } }}>
                  <MenuIcon fontSize="large" />
                </IconButton>
                <Box sx={{ display: { xs: "none", sm: "block" } }}>
                  <img
                    className="vendor-logo"
                    src={clientSettings?.vendor?.logo}
                    alt={`Logo of ${clientSettings?.vendor?.name}`}
                    onError={e => {
                      e.target.style.display = "none";
                    }}
                  />
                </Box>
              </>
            ) : (
              <img
                className="vendor-logo"
                src={clientSettings?.vendor?.logo}
                alt={`Logo of ${clientSettings?.vendor?.name}`}
                onError={e => {
                  e.target.style.display = "none";
                }}
              />
            )}
            <Typography variant="h6" component="div" sx={{ display: { xs: "none", sm: "block" }, flexGrow: 1 }}>
              {location.pathname.includes("admin") ? t("administration").toUpperCase() : t("delivery").toUpperCase()}
            </Typography>
          </Box>
          <Box sx={{ flexGrow: 0 }}>
            <LoggedOutTemplate>
              <Button className="nav-button" sx={{ color: "white" }} onClick={login}>
                {t("logIn")}
              </Button>
            </LoggedOutTemplate>
            <LoggedInTemplate>
              <IconButton className="nav-button" sx={{ color: "white" }} onClick={toggleUserMenu(true)}>
                <AccountCircleOutlinedIcon fontSize="large" />
              </IconButton>
            </LoggedInTemplate>
          </Box>
        </Toolbar>
      </AppBar>
      <Drawer anchor={"right"} open={userMenuOpen} onClose={toggleUserMenu(false)}>
        <div className="user-menu">
          <Box
            sx={{ width: 250 }}
            role="presentation"
            onClick={toggleUserMenu(false)}
            onKeyDown={toggleUserMenu(false)}>
            <List>
              <ListItem key={user?.name}>
                <ListItemText primary={user?.name} />
              </ListItem>
            </List>
            <Divider />
            <List>
              <ListItem key={t("delivery").toUpperCase()} disablePadding>
                <ListItemButton
                  onClick={() => {
                    navigate("/");
                  }}>
                  <ListItemText primary={t("delivery").toUpperCase()} />
                </ListItemButton>
              </ListItem>
              <AdminTemplate>
                <ListItem key={t("administration").toUpperCase()} disablePadding>
                  <ListItemButton
                    onClick={() => {
                      navigate("/admin");
                    }}>
                    <ListItemText primary={t("administration").toUpperCase()} />
                  </ListItemButton>
                </ListItem>
                <ListItem key={t("stacBrowser").toUpperCase()} disablePadding>
                  <ListItemButton
                    onClick={() => {
                      window.location.href = "/browser";
                    }}>
                    <ListItemText primary={t("stacBrowser").toUpperCase()} />
                  </ListItemButton>
                </ListItem>
              </AdminTemplate>
            </List>
          </Box>
          <Button className="nav-button" sx={{ color: "black" }} onClick={logout}>
            {t("logOut")}
          </Button>
        </div>
      </Drawer>
    </>
  );
};

export default Header;
