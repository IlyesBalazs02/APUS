export interface PagedResponse<T> {
    items: T[];
    hasMore: boolean;
}