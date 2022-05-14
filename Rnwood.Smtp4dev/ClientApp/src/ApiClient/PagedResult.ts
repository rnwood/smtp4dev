export default class PagedResult<Type> {
  constructor(
    currentPage: number,
    firstRowOnPage: number,
    lastRowOnPage: number,
    pageCount: number,
    pageSize: number,
    rowCount: number,
    results: Array<Type>,
  ) {
    
    this.currentPage = currentPage;
    this.firstRowOnPage = firstRowOnPage;
    this.lastRowOnPage = lastRowOnPage;
    this.pageCount = pageCount;
    this.pageSize = pageSize;
    this.rowCount = rowCount;
    this.results = results;
  }

  currentPage: number;
  firstRowOnPage: number;
  lastRowOnPage: number;
  pageCount: number;
  rowCount: number;
  pageSize: number;
  results: Array<Type>;
}
