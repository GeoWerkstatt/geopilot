import { defineConfig } from "cypress";
import vitePreprocessor from "cypress-vite";

export default defineConfig({
  projectId: "bqtbpp",
  e2e: {
    baseUrl: "https://localhost:5173",
    video: false,
    viewportWidth: 1920,
    viewportHeight: 1080,
    supportFile: "cypress/support/e2e.js",
    setupNodeEvents(on) {
      on("file:preprocessor", vitePreprocessor());

      on("task", {
        log(message) {
          console.log(message);

          return null;
        },
      });

      on("before:browser:launch", (browser, launchOptions) => {
        launchOptions.preferences.width = 1920;
        launchOptions.preferences.height = 1080;
        launchOptions.preferences.frame = false;
        launchOptions.preferences.useContentSize = true;

        return launchOptions;
      });
    },
  },
  component: {
    devServer: {
      framework: "react",
      bundler: "vite",
    },
    supportFile: "cypress/support/component.js",
  },
  defaultCommandTimeout: 10000,
  waitForAnimations: false,
  animationDistanceThreshold: 50,
  retries: 3,
});
