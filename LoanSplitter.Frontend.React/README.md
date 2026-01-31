# LoanSplitter Frontend (React)

Modern React frontend for the LoanSplitter API.

## Tech Stack

- **React 18** - UI library
- **Vite** - Build tool and dev server
- **Tailwind CSS** - Utility-first CSS framework

## Development

```bash
# Install dependencies
npm install

# Run dev server (http://localhost:5173)
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

## Usage

1. Make sure the backend API is running at `http://localhost:5000`
2. Start the dev server with `npm run dev`
3. Open `http://localhost:5173` in your browser
4. Upload a JSON event file (e.g., `../LoanSplitter/sample-events.json`)
5. Select a loan name and snapshot date
6. View the loan summary and payment breakdown

## API Configuration

The API base URL is configured in `src/App.jsx`. Change the `API_BASE_URL` constant if your backend runs on a different port.

## Project Structure

```
src/
  App.jsx          - Main application component
  index.css        - Tailwind CSS imports and global styles
  main.jsx         - React entry point
```
