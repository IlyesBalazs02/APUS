import { Injectable } from '@angular/core';
import * as ExifReader from 'exifreader';

export interface ExifMetadata {
    dateTaken?: string | null;   // raw EXIF format, sent to backend
}

@Injectable({ providedIn: 'root' })
export class ExifService {

    /**
     * Extract EXIF metadata from a single image.
     */
    async extractFromFile(file: File): Promise<ExifMetadata | null> {
        try {
            const tags = await ExifReader.load(file);

            const date =
                tags['DateTime']?.description ||
                tags['DateTimeOriginal']?.description ||
                tags['CreateDate']?.description ||
                null;

            return {
                dateTaken: date
            };
        } catch (err) {
            console.warn('EXIF load failed:', err);
            return null;
        }
    }

    /**
     * Extract EXIF for many files.
     * Returns: Map<filename, ExifMetadata>
     */
    async extractMany(files: File[]): Promise<Map<string, ExifMetadata>> {
        const map = new Map<string, ExifMetadata>();

        for (const file of files) {
            const meta = await this.extractFromFile(file);
            if (meta && meta.dateTaken) {
                map.set(file.name, meta);
            }
        }

        return map;
    }
}
