export default function sortedArraySync(source: any[], target: any[], comparer: (a:any, b:any) => boolean) : void {

    for (let deleted of target.filter(m => source.findIndex(v => comparer(m, v)) === -1)) {
        target.splice(target.findIndex(m => comparer(m, deleted)), 1);
    }

    for (let added of source.filter(m => target.findIndex(v => comparer(m, v)) === -1)) {
        target.splice(source.findIndex(m => comparer(m, added)), 0, added);
    }

}