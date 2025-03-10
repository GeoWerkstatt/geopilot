import { Typography } from "@mui/material";
import { useTranslation } from "react-i18next";
import { useEffect, useState } from "react";
import { useApi } from "../../api";
import { MarkdownContent } from "../../components/markdownContent.tsx";
import { ContentType } from "../../api/apiInterfaces.ts";
import { CenteredBox } from "../../components/styledComponents.ts";

export const Imprint = () => {
  const { t, i18n } = useTranslation();
  const [content, setContent] = useState<string>();
  const { fetchApi } = useApi();

  useEffect(() => {
    fetchApi<string>(`/imprint.${i18n.language}.md`, { responseType: ContentType.Markdown })
      .then(response => {
        if (response) {
          setContent(response);
        } else {
          throw new Error("Language-specific imprint not found");
        }
      })
      .catch(() => {
        fetchApi<string>("/imprint.md", { responseType: ContentType.Markdown }).then(setContent);
      });
  }, [fetchApi, i18n.language]);

  return (
    <CenteredBox>
      {content ? (
        <MarkdownContent content={content} />
      ) : (
        <>
          <Typography variant="h1">{t("imprint")}</Typography>
          <p>{t("contentNotFound")}</p>
        </>
      )}
    </CenteredBox>
  );
};
