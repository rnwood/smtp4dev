module.exports = {
    testEnvironment: 'jsdom',
    moduleNameMapper: {
      '^@/(.*)$': '<rootDir>/$1',
      '^~/(.*)$': '<rootDir>/$1'
    },
    moduleFileExtensions: ['ts', 'js', 'vue', 'json'],
    transform: {
      '^.+\\.ts$': 'ts-jest',
      '^.+\\.js$': 'babel-jest',
      '.*\\.(vue)$': 'vue-jest'
    },
    transformIgnorePatterns: [
      '/node_modules/',
      '/vendor/',
    ],
    collectCoverageFrom: [
      '<rootDir>/components/**/*.vue',
      '<rootDir>/layouts/**/*.vue',
      '<rootDir>/middleware/**/*.vue',
      '<rootDir>/middleware/**/*.ts',
      '<rootDir>/mixins/**/*.vue',
      '<rootDir>/mixins/**/*.ts',
      '<rootDir>/pages/**/*.vue',
      '<rootDir>/plugins/**/*.ts',
      '<rootDir>/schemes/**/*.ts',
      '<rootDir>/store/**/*.ts',
      '<rootDir>/utils/**/*.ts'
    ],
    setupFiles: ['jest-canvas-mock', './jest.setup.ts']
  };