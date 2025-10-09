import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SentimentResponse } from '../models/sentiment-response.model';

@Injectable({
  providedIn: 'root'
})
export class SentimentService {
  private apiUrl = 'http://localhost:5000/api/sentiment/analyze';

  constructor(private http: HttpClient) {}

  analyzeSentiment(text: string): Observable<SentimentResponse> {
    const requestBody = { text };
    return this.http.post<SentimentResponse>(this.apiUrl, requestBody);
  }
}