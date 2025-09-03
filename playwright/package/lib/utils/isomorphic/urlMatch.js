"use strict";

Object.defineProperty(exports, "__esModule", {
  value: true
});
exports.constructURLBasedOnBaseURL = constructURLBasedOnBaseURL;
exports.globToRegex = globToRegex;
exports.urlMatches = urlMatches;
exports.urlMatchesEqual = urlMatchesEqual;
var _stringUtils = require("./stringUtils");
/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Regular_expressions#escaping
const escapedChars = new Set(['$', '^', '+', '.', '*', '(', ')', '|', '\\', '?', '{', '}', '[', ']']);
function globToRegex(glob) {
  const tokens = ['^'];
  let inGroup = false;
  for (let i = 0; i < glob.length; ++i) {
    const c = glob[i];
    if (c === '\\' && i + 1 < glob.length) {
      const char = glob[++i];
      tokens.push(escapedChars.has(char) ? '\\' + char : char);
      continue;
    }
    if (c === '*') {
      const beforeDeep = glob[i - 1];
      let starCount = 1;
      while (glob[i + 1] === '*') {
        starCount++;
        i++;
      }
      const afterDeep = glob[i + 1];
      const isDeep = starCount > 1 && (beforeDeep === '/' || beforeDeep === undefined) && (afterDeep === '/' || afterDeep === undefined);
      if (isDeep) {
        tokens.push('((?:[^/]*(?:\/|$))*)');
        i++;
      } else {
        tokens.push('([^/]*)');
      }
      continue;
    }
    switch (c) {
      case '?':
        tokens.push('.');
        break;
      case '[':
        tokens.push('[');
        break;
      case ']':
        tokens.push(']');
        break;
      case '{':
        inGroup = true;
        tokens.push('(');
        break;
      case '}':
        inGroup = false;
        tokens.push(')');
        break;
      case ',':
        if (inGroup) {
          tokens.push('|');
          break;
        }
        tokens.push('\\' + c);
        break;
      default:
        tokens.push(escapedChars.has(c) ? '\\' + c : c);
    }
  }
  tokens.push('$');
  return new RegExp(tokens.join(''));
}
function isRegExp(obj) {
  return obj instanceof RegExp || Object.prototype.toString.call(obj) === '[object RegExp]';
}
function urlMatchesEqual(match1, match2) {
  if (isRegExp(match1) && isRegExp(match2)) return match1.source === match2.source && match1.flags === match2.flags;
  return match1 === match2;
}
function urlMatches(baseURL, urlString, match) {
  if (match === undefined || match === '') return true;
  if ((0, _stringUtils.isString)(match) && !match.startsWith('*')) match = constructURLBasedOnBaseURL(baseURL, match);
  if ((0, _stringUtils.isString)(match)) match = globToRegex(match);
  if (isRegExp(match)) return match.test(urlString);
  if (typeof match === 'string' && match === urlString) return true;
  const url = parsedURL(urlString);
  if (!url) return false;
  if (typeof match === 'string') return url.pathname === match;
  if (typeof match !== 'function') throw new Error('url parameter should be string, RegExp or function');
  return match(url);
}
function parsedURL(url) {
  try {
    return new URL(url);
  } catch (e) {
    return null;
  }
}
function constructURLBasedOnBaseURL(baseURL, givenURL) {
  try {
    return new URL(givenURL, baseURL).toString();
  } catch (e) {
    return givenURL;
  }
}