export class SentimentAnalyzerComponent {
  userInput: string = '';
  sentimentResult: string | null = null;

  constructor(private sentimentService: SentimentService) {}

  analyzeSentiment() {
    if (this.userInput.trim()) {
      this.sentimentService.analyzeSentiment(this.userInput).subscribe(
        (response) => {
          this.sentimentResult = response.sentiment;
        },
        (error) => {
          console.error('Error analyzing sentiment:', error);
          this.sentimentResult = 'Error analyzing sentiment. Please try again.';
        }
      );
    } else {
      this.sentimentResult = 'Please enter some text to analyze.';
    }
  }
}