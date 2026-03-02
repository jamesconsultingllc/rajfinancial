module.exports = {
  default: {
    requireModule: ["ts-node/register"],
    require: ["support/**/*.ts", "step-definitions/**/*.ts"],
    paths: ["features/**/*.feature"],
    format: [
      "progress-bar",
      "html:reports/cucumber-report.html",
      "json:reports/cucumber-report.json",
    ],
    formatOptions: { snippetInterface: "async-await" },
    worldParameters: {},
  },
};
