const { defineConfig } = require('@vue/cli-service')
module.exports = defineConfig({
  devServer: {    
    onAfterSetupMiddleware() { // Output the same message as the react dev server to get the Spa middleware working with vue.
      console.info("Starting the development server...");
    }
  },
  transpileDependencies: true
})
