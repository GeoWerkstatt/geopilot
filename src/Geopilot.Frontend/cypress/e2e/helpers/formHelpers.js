import { createBaseSelector } from "./appHelpers";

/**
 * Checks if a form element has an error.
 * @param {string} fieldName The name of the form element.
 * @param {boolean} hasError The expected error state.
 * @param {string} parent  (optional) The parent of the form element.
 */
export const hasError = (fieldName, hasError = true, parent) => {
  const selector = createBaseSelector(parent) + `[data-cy^="${fieldName}-form"] .Mui-error`;
  if (hasError) {
    cy.get(selector).should("exist");
  } else {
    cy.get(selector).should("not.exist");
  }
};

/**
 * Sets the value for an input form element.
 * @param {string} fieldName The name of the input field.
 * @param {string} value The text to type into the input field.
 * @param {string} parent (optional) The parent of the form element.
 */
export const setInput = (fieldName, value, parent) => {
  const selector = createBaseSelector(parent) + `[data-cy="${fieldName}-formInput"]`;
  cy.get(selector)
    .click()
    .then(() => {
      cy.focused().clear();
      cy.get(selector).type(value, {
        delay: 10,
      });
    });
};

/**
 * Opens the dropdown for a select form element.
 * @param {string} selector The selector for the form element.
 */
export const openDropdown = selector => {
  cy.get(selector).find('[role="combobox"]').click();
};

/**
 * Selects an option from a dropdown.
 * @param {number} index The index of the option to select.
 */
export const selectDropdownOption = index => {
  cy.get('.MuiPaper-elevation [role="listbox"]').find('[role="option"]').eq(index).click();
};

/**
 * Evaluates the number of options in a dropdown.
 * @param {number} length The expected number of options in the dropdown.
 */
export const evaluateDropdownOptionsLength = length => {
  cy.get('.MuiPaper-elevation [role="listbox"]').should($listbox => {
    expect($listbox.find('[role="option"]')).to.have.length(length);
  });
};

/**
 * Sets the value for a select form element.
 * @param {string} fieldName The name of the select field.
 * @param {number} index The index of the option to select.
 * @param {number} expected (optional) The expected number of options in the dropdown.
 * @param {string} parent (optional) The parent of the form element.
 */
export const setSelect = (fieldName, index, expected, parent) => {
  const selector = createBaseSelector(parent) + `[data-cy="${fieldName}-formSelect"]`;
  openDropdown(selector);
  if (expected != null) {
    evaluateDropdownOptionsLength(expected);
  }
  selectDropdownOption(index);
};

/**
 * Toggles the checkbox for a checkbox form element.
 * @param {string} fieldName The name of the checkbox field.
 * @param {string} parent (optional) The parent of the form element.
 */
export const toggleCheckbox = (fieldName, parent) => {
  const selector = createBaseSelector(parent) + `[data-cy="${fieldName}-formCheckbox"]`;
  cy.get(selector).click();
};