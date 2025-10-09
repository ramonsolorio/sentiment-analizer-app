export class SentimentService {
  private apiUrl = 'http://localhost:5000/api/sentiment'; // Update with your backend API URL

  constructor(private http: HttpClient) {}

  analyzeSentiment(text: string): Observable<SentimentResponse> {
    const requestBody = { text };
    return this.http.post<SentimentResponse>(this.apiUrl, requestBody);
  }
}