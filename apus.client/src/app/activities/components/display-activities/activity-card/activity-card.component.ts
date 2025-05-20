import { Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { ActivityDto, DisplayProp } from '../../../ActivityDto/ActivityDto';
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
  trackImage: string = '';
  selectedIndex: number | null = null;

  constructor(private http: HttpClient) { }

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
      .get<string[]>(`/api/images/${this.activity.id}`)
      .subscribe(
        urls => this.images = urls,
        err => console.error('gallery load failed', err)
      );

    // 2) Load the single track‐png URL
    this.http
      .get(`/api/images/${this.activity.id}/track`, { responseType: 'text' })
      .subscribe(
        url => {
          this.trackImage = url;
          console.log('trackImage URL is', url);
        },
        err => console.error('track‐png load failed', err)
      );
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
