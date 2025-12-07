import { Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { ActivityDto, DisplayProp } from '../../features/activities/ActivityDto/ActivityDto';
import { HttpClient } from '@angular/common/http';

type PropMap = Record<string, { key: keyof ActivityDto, label: string }[]>;

@Component({
  selector: 'app-activity-card',
  standalone: false,
  templateUrl: './activity-card.component.html',
  styleUrls: ['./activity-card.component.scss']
})
export class ActivityCardComponent implements OnChanges, OnInit {
  @Input() activity!: ActivityDto;

  public displayProps: DisplayProp[] = [];

  images: string[] = [];
  public trackImage: string | null = null;
  selectedIndex: number | null = null;

  commentsOpen = false;

  constructor(private http: HttpClient) { }

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
    // 1) Load the gallery of images
    this.http
      .get<string[]>(`/api/images/${this.activity.id}/urls`)
      .subscribe(
        urls => this.images = urls,
        err => console.error('gallery load failed', err)
      );

    // 2) Load the single track‐png URL
    this.http
      .get(`/api/images/${this.activity.id}/track`, { responseType: 'text' })
      .subscribe(
        url => {
          // quick preload
          const img = new Image();
          img.onload = () => { this.trackImage = url; };
          img.onerror = () => { /* nothing: leave trackImage null */ };
          img.src = url;
        },
        _ => {
          // API 404 or network error → definitely no map
          this.trackImage = null;
        }
      );
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['activity'] && this.activity) {
      this.displayProps = this.buildDisplayProps(this.activity);
      this.displayProps.push()
    }
  }

  private buildDisplayProps(a: ActivityDto): DisplayProp[] {
    const typeKey = (a as any).activityType ?? 'default';
    const entries = this.propMap[typeKey] || this.propMap['default'];

    return entries
      .filter(({ key }) => a[key] != null)
      .map(({ key, label }) => {
        const raw = a[key];
        let value: string | number = raw as any;

        if (key === 'pace' && typeof raw === 'number') {
          value = this.formatPace(raw);
        }

        return { label, value };
      });
  }

  private formatPace(speed: number | null | undefined): string {
    if (speed == null || speed <= 0) {
      return '-';
    }

    const secondsPerKm = 1000 / speed; // speed is m/s
    const minutes = Math.floor(secondsPerKm / 60);
    const seconds = Math.round(secondsPerKm % 60);
    const secStr = seconds.toString().padStart(2, '0');

    return `${minutes}:${secStr}`;
  }


  openCommentsModal() {
    this.commentsOpen = true;
  }

  closeCommentsModal() {
    this.commentsOpen = false;
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
