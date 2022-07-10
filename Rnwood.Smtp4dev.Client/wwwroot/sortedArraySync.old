export default function sortedArraySync(source: any[], target: any[], comparer: (a:any, b:any) => boolean, updater: ((a:any, b:any) => void)|null = null) : void {

    if (updater) {
        for (const sourceItem of source) {
            const targetItem = target.find(i => comparer(i, sourceItem));
            if (targetItem) {
                updater(sourceItem, targetItem);
            }
        }
    }

    for (const deleted of target.filter(m => source.findIndex(v => comparer(m, v)) === -1)) {
        target.splice(target.findIndex(m => comparer(m, deleted)), 1);
    }

    for (const added of source.filter(m => target.findIndex(v => comparer(m, v)) === -1)) {
        target.splice(source.findIndex(m => comparer(m, added)), 0, added);
    }

}