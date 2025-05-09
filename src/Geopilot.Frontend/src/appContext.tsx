import { I18nextProvider } from "react-i18next";
import i18n from "./i18n";
import { GlobalStyles, ThemeProvider } from "@mui/material";
import { PromptProvider } from "./components/prompt/promptContext.tsx";
import { Prompt } from "./components/prompt/prompt.tsx";
import { AlertProvider } from "./components/alert/alertContext.tsx";
import { AlertBanner } from "./components/alert/alertBanner.tsx";
import { AppSettingsProvider } from "./components/appSettings/appSettingsContext.tsx";
import { GeopilotAuthProvider } from "./auth/geopilotAuthComponent.tsx";
import App from "./app.tsx";
import { useEffect, useState } from "react";
import { Language } from "./appInterfaces.ts";
import { geopilotTheme } from "./appTheme.ts";
import { deDE as coreDe, enUS as coreEn, frFR as coreFr, itIT as coreIt } from "@mui/material/locale";
import { enUS as gridEn } from "@mui/x-data-grid/locales/enUS";
import { deDE as gridDe } from "@mui/x-data-grid/locales/deDE";
import { frFR as gridFr } from "@mui/x-data-grid/locales/frFR";
import { itIT as gridIt } from "@mui/x-data-grid/locales/itIT";
import { AppTheme, createTheme } from "@mui/material/styles";

export const AppContext = () => {
  const [language, setLanguage] = useState<Language>(Language.EN);
  const [theme, setTheme] = useState<AppTheme>(geopilotTheme);

  useEffect(() => {
    let coreLng = coreEn;
    let gridLng = gridEn;
    switch (language) {
      case Language.DE:
        coreLng = coreDe;
        gridLng = gridDe;
        break;
      case Language.FR:
        coreLng = coreFr;
        gridLng = gridFr;
        break;
      case Language.IT:
        coreLng = coreIt;
        gridLng = gridIt;
        break;
      case Language.EN:
        coreLng = coreEn;
        gridLng = gridEn;
        break;
    }
    setTheme(createTheme(geopilotTheme, gridLng, coreLng) as unknown as AppTheme);
  }, [language]);

  useEffect(() => {
    const handleLanguageChange = (lng: Language) => {
      setLanguage(lng);
    };

    i18n.on("languageChanged", handleLanguageChange);

    return () => {
      i18n.off("languageChanged", handleLanguageChange);
    };
  }, []);

  return (
    <I18nextProvider i18n={i18n}>
      <ThemeProvider theme={theme}>
        <GlobalStyles
          styles={{
            ":root": {
              fontFamily: '"NeoGeo", sans-serif',
              fontSize: "16px",
              letterSpacing: "0.05em",
            },
          }}
        />
        <PromptProvider>
          <Prompt />
          <AlertProvider>
            <AlertBanner />
            <AppSettingsProvider>
              <GeopilotAuthProvider>
                <App />
              </GeopilotAuthProvider>
            </AppSettingsProvider>
          </AlertProvider>
        </PromptProvider>
      </ThemeProvider>
    </I18nextProvider>
  );
};
