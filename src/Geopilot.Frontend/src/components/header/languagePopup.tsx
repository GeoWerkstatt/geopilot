import { MouseEvent, useEffect, useState } from "react";
import CheckIcon from "@mui/icons-material/Check";
import ExpandLessIcon from "@mui/icons-material/ExpandLess";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import { Button, List, ListItem, ListItemIcon, ListItemText, Popover } from "@mui/material";
import { Language } from "../../appInterfaces";
import { geopilotTheme } from "../../appTheme.ts";
import i18n from "../../i18n";

const defaultLanguage = Language.DE;

export function LanguagePopup() {
  const [selectedLanguage, setSelectedLanguage] = useState(defaultLanguage);
  const [anchorEl, setAnchorEl] = useState<HTMLButtonElement>();
  const isOpen = Boolean(anchorEl);
  const languages: string[] = Object.values(Language);

  const handleClick = (event: MouseEvent<HTMLButtonElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(undefined);
  };

  useEffect(() => {
    const handleLanguageChange = () => {
      const languageIndex = languages.indexOf(i18n.language);
      if (languageIndex !== -1) {
        setSelectedLanguage(languages[languageIndex] as Language);
      } else {
        setSelectedLanguage(defaultLanguage);
      }
    };
    handleLanguageChange();

    i18n.on("languageChanged", handleLanguageChange);

    return () => {
      i18n.off("languageChanged", handleLanguageChange);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const onLanguageChanged = (language: string) => {
    i18n.changeLanguage(language);
    handleClose();
  };

  return (
    <>
      <Button
        onClick={handleClick}
        endIcon={anchorEl ? <ExpandLessIcon /> : <ExpandMoreIcon />}
        sx={{
          marginRight: "10px",
          ...(isOpen && { backgroundColor: geopilotTheme.palette.primary.hover }),
        }}
        data-cy="language-selector">
        {selectedLanguage.toUpperCase()}
      </Button>
      <Popover
        anchorEl={anchorEl}
        open={isOpen}
        onClose={handleClose}
        sx={{ marginTop: "5px" }}
        anchorOrigin={{
          vertical: "bottom",
          horizontal: "right",
        }}
        transformOrigin={{
          vertical: "top",
          horizontal: "right",
        }}>
        <List sx={{ padding: 0 }}>
          {languages.map(language => (
            <ListItem
              key={language}
              data-cy={`language-${language}`}
              onClick={() => {
                onLanguageChanged(language);
              }}
              sx={{
                cursor: "pointer",
                "&:hover": { backgroundColor: geopilotTheme.palette.primary.hover },
              }}>
              {selectedLanguage === language && (
                <ListItemIcon sx={{ minWidth: "20px" }}>
                  <CheckIcon fontSize="small" sx={{ mr: 1 }} />
                </ListItemIcon>
              )}
              <ListItemText
                sx={{
                  textAlign: "right",
                  minWidth: "24px",
                  paddingLeft: "5px",
                  fontSize: "14px",
                }}>
                {language.toUpperCase()}
              </ListItemText>
            </ListItem>
          ))}
        </List>
      </Popover>
    </>
  );
}
