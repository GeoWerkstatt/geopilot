import { useContext } from "react";
import { Button, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle } from "@mui/material";
import { PromptContext } from "./promptContext";

export const Prompt = () => {
  const { promptIsOpen, title, message, actions, closePrompt } = useContext(PromptContext);
  return (
    <Dialog open={promptIsOpen} data-cy="prompt">
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <DialogContentText>{message}</DialogContentText>
      </DialogContent>
      <DialogActions>
        {actions?.map((action, index) => (
          <Button
            key={index}
            onClick={() => {
              if (action.action != null) {
                action.action();
              }
              closePrompt();
            }}
            color={action.color ? action.color : "primary"}
            disabled={action.disabled === true}
            data-cy={"prompt-button-" + action.label}>
            {action.label}
          </Button>
        ))}
      </DialogActions>
    </Dialog>
  );
};
