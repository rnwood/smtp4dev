/**
 * Sort array of objects based on another array
 */
export function mapOrder<T>(array: T[], order: unknown[], key: keyof T) {
  array.sort(function(a, b) {
    const A = a[key],
      B = b[key];

    if (order.indexOf(A) > order.indexOf(B)) {
      return 1;
    } else {
      return -1;
    }
  });

  return array;
}
