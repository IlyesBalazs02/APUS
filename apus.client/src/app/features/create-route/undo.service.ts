import { Injectable } from '@angular/core';

export type UndoAction =
    | { kind: 'add-point' }
    | { kind: 'move-point'; index: number; from: { lat: number; lon: number }; to: { lat: number; lon: number } };

export interface UndoContext {
    snappedPoints: { lat: number; lon: number }[];
    routeSegments: { coords: { lat: number; lon: number }[]; isOutAndBack: boolean }[];
    fullRouteCoords: { lat: number; lon: number }[];

    updatePointsSource(): void;
    updateRouteSourceEmpty(): void;
    rebuildRouteFromSegments(): void;
    clearProfiles(): void;
    recalculateRouteForAllPoints(): void;
}

@Injectable({ providedIn: 'root' })
export class UndoService {
    private undoStack: UndoAction[] = [];

    canUndo(): boolean {
        return this.undoStack.length > 0;
    }

    reset(): void {
        this.undoStack = [];
    }

    pushAddPoint(): void {
        this.undoStack.push({ kind: 'add-point' });
    }

    pushMovePoint(index: number, from: { lat: number; lon: number }, to: { lat: number; lon: number }): void {
        this.undoStack.push({ kind: 'move-point', index, from, to });
    }

    undoLast(ctx: UndoContext): void {
        if (this.undoStack.length === 0) {
            return;
        }

        const action = this.undoStack.pop()!;

        switch (action.kind) {
            case 'add-point': {
                if (ctx.snappedPoints.length === 0) {
                    return;
                }

                // Remove last snapped point
                ctx.snappedPoints.pop();
                ctx.updatePointsSource();

                if (ctx.snappedPoints.length < 2) {
                    // No route possible anymore
                    ctx.routeSegments.splice(0, ctx.routeSegments.length);
                    ctx.fullRouteCoords.splice(0, ctx.fullRouteCoords.length);
                    ctx.updateRouteSourceEmpty();
                    ctx.clearProfiles();
                } else {
                    // Remove last forward segment (ignore Out & Back if present)
                    if (ctx.routeSegments.length > 0) {
                        const lastIdx = ctx.routeSegments.length - 1;
                        const last = ctx.routeSegments[lastIdx];

                        if (!last.isOutAndBack) {
                            ctx.routeSegments.pop();
                        } else if (ctx.routeSegments.length > 1) {
                            // If last is Out & Back, drop it and the previous forward segment
                            ctx.routeSegments.pop(); // Out & Back
                            ctx.routeSegments.pop(); // previous forward
                        }
                    }

                    ctx.rebuildRouteFromSegments();
                }

                break;
            }

            case 'move-point': {
                const { index, from } = action;

                if (index < 0 || index >= ctx.snappedPoints.length) {
                    return;
                }

                // Move point back
                ctx.snappedPoints[index] = { ...from };
                ctx.updatePointsSource();

                // Recalculate route geometry based on current points
                ctx.recalculateRouteForAllPoints();
                break;
            }
        }
    }
}
