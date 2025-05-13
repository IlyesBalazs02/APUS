export interface Trackpoint {
    time: string;  // or: time: Date;
    lat?: number;
    lon?: number;
    hr?: number;
    pace?: number;
    alt?: number;
}
