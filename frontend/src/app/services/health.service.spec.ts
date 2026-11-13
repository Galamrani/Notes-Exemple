import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { HealthService } from './health.service';

describe('HealthService', () => {
  let service: HealthService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(HealthService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('reports online when the API answers 200 with status ok', () => {
    const results: boolean[] = [];
    service.isOnline().subscribe((online) => results.push(online));

    const request = httpTesting.expectOne('/api/health');
    expect(request.request.method).toBe('GET');
    request.flush({ status: 'ok' });

    expect(results).toEqual([true]);
  });

  it('reports offline when the API answers 200 with a status other than ok', () => {
    const results: boolean[] = [];
    service.isOnline().subscribe((online) => results.push(online));

    httpTesting.expectOne('/api/health').flush({ status: 'degraded' });

    expect(results).toEqual([false]);
  });

  it('reports offline instead of erroring when the request fails', () => {
    const results: boolean[] = [];
    let errored = false;
    service.isOnline().subscribe({
      next: (online) => results.push(online),
      error: () => (errored = true),
    });

    httpTesting
      .expectOne('/api/health')
      .flush('Service unavailable', { status: 503, statusText: 'Service Unavailable' });

    expect(results).toEqual([false]);
    expect(errored).toBe(false);
  });

  it('reports offline when the network request errors', () => {
    const results: boolean[] = [];
    service.isOnline().subscribe((online) => results.push(online));

    httpTesting.expectOne('/api/health').error(new ProgressEvent('error'));

    expect(results).toEqual([false]);
  });
});
