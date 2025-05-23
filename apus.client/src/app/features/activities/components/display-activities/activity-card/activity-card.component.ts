import { Component, Inject, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { ActivityDto, DisplayProp } from '../../../ActivityDto/ActivityDto';
import { HttpClient } from '@angular/common/http';
import { catchError, of } from 'rxjs';
import { MainActivity } from '../../../_models/ActivityClasses';

type PropMap = Record<string, { key: keyof ActivityDto, label: string }[]>;

@Component({
  selector: 'app-activity-card',
  standalone: false,
  templateUrl: './activity-card.component.html',
  styleUrls: ['./activity-card.component.scss']
})
export class ActivityCardComponent implements OnChanges, OnInit {

  public displayProps: DisplayProp[] = [];

  images: string[] = [];
  trackImage: string = '';
  selectedIndex: number | null = null;

  constructor(private http: HttpClient,
    @Inject('activity') private activity: MainActivity) { }

  //Which properties to display
  private readonly propMap: PropMap = {
    Running: [
      { key: 'pace', label: 'Pace' },
      { key: 'distanceKm', label: 'Distance (km)' },
    ],
    GpsRelatedActivity: [
      { key: 'distanceKm', label: 'Distance (km)' },
      { key: 'elevationGain', label: 'Elevation (m)' },
    ],
    'default': [
      { key: 'avgHr', label: 'Avg HR' },
      { key: 'totalCalories', label: 'Calories' },
    ],
  };

  ngOnInit(): void {
    // 1) Load the gallery
    this.http
      .get<string[]>(`/api/images/${this.activity.id}`)
      .pipe(
        catchError(err => {
          if (err.status === 404) {
            // no images for this activity → just use empty array
            return of([]);
          }
          // some other error → rethrow/log then fallback
          console.error('Gallery load failed:', err);
          return of([]);
        })
      )
      .subscribe(urls => this.images = urls);

    // 2) Load the track‐png URL
    this.http
      .get<string>(`/api/images/${this.activity.id}/track`, { responseType: 'text' })
      .pipe(
        catchError(err => {
          if (err.status === 404) {
            // no track image → leave trackImage blank (or point at a placeholder)
            return of('');
          }
          console.error('Track‐png load failed:', err);
          return of('');
        })
      )
      .subscribe(url => this.trackImage = url);
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['activity'] && this.activity) {
      this.displayProps = this.buildDisplayProps(this.activity);
      this.displayProps.push()
    }
  }

  private buildDisplayProps(a: ActivityDto): DisplayProp[] {
    const entries = this.propMap[a.type] || this.propMap['default'];

    return entries
      .filter(({ key }) => a[key] != null)
      .map(({ key, label }) => ({
        label,
        value: a[key] as string | number
      }));
  }

  openViewer(i: number) {
    this.selectedIndex = i;
  }

  prevImage(event: MouseEvent) {
    event.stopPropagation();
    if (this.selectedIndex === null) return;
    const len = this.images.length;
    this.selectedIndex = (this.selectedIndex + len - 1) % len;
  }

  nextImage(event: MouseEvent) {
    event.stopPropagation();
    if (this.selectedIndex === null) return;
    const len = this.images.length;
    this.selectedIndex = (this.selectedIndex + 1) % len;
  }

  closeViewer() {
    this.selectedIndex = null;
  }
}
