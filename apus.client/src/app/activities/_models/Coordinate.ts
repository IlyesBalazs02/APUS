export interface Coordinate {
    latitude: number;
    longitude: number;
    altitude?: number | null;
    time?: Date | null;
}