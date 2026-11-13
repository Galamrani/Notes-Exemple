import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of } from 'rxjs';
import { HealthService } from '../services/health.service';
import { Home } from './home';

function renderWith(isOnline: () => Observable<boolean>) {
  TestBed.configureTestingModule({
    imports: [Home],
    providers: [{ provide: HealthService, useValue: { isOnline } }],
  });
  const fixture = TestBed.createComponent(Home);
  fixture.detectChanges();
  return fixture;
}

function statusText(element: HTMLElement): string {
  return element.querySelector('.api-status')?.textContent?.trim() ?? '';
}

describe('Home', () => {
  it('shows API: online when the API is online', async () => {
    const fixture = renderWith(() => of(true));
    await fixture.whenStable();

    expect(statusText(fixture.nativeElement)).toBe('API: online');
  });

  it('shows API: offline when the API is offline', async () => {
    const fixture = renderWith(() => of(false));
    await fixture.whenStable();

    expect(statusText(fixture.nativeElement)).toBe('API: offline');
  });

  it('shows API: checking… while waiting for the API', async () => {
    const pending = new Subject<boolean>();
    const fixture = renderWith(() => pending);
    await fixture.whenStable();

    expect(statusText(fixture.nativeElement)).toBe('API: checking…');
  });
});
