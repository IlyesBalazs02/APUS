import { Injectable } from '@angular/core';

export interface GpxCoordinate {
    lat: number;
    lon: number;
}

@Injectable({ providedIn: 'root' })
export class GpxImportService {
    parseGpx(file: File): Promise<GpxCoordinate[]> {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();

            reader.onerror = () => reject(reader.error);

            reader.onload = () => {
                try {
                    const text = reader.result as string;
                    const parser = new DOMParser();
                    const doc = parser.parseFromString(text, 'application/xml');

                    const pts = Array.from(doc.getElementsByTagName('trkpt'));
                    if (pts.length === 0) {
                        resolve([]);
                        return;
                    }

                    const coords: GpxCoordinate[] = [];

                    for (const tp of pts) {
                        const latStr = tp.getAttribute('lat');
                        const lonStr = tp.getAttribute('lon');

                        if (!latStr || !lonStr) {
                            continue;
                        }

                        const lat = parseFloat(latStr);
                        const lon = parseFloat(lonStr);

                        if (Number.isNaN(lat) || Number.isNaN(lon)) {
                            continue;
                        }

                        coords.push({ lat, lon });
                    }

                    resolve(coords);
                } catch (err) {
                    reject(err);
                }
            };

            reader.readAsText(file);
        });
    }
}
