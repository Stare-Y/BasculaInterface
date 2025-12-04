import { useState ,useEffect} from 'react'
import './App.css'
import AppRoutes from './routes';
import { ThemeProvider } from '@emotion/react';
import theme from "./theme.js";


function App() {
    // const dispatch = useDispatch();
    const [isAppReady, setIsAppReady] = useState(false);
    useEffect(() => {
       
        setIsAppReady(true);
    }, []);

  return (
    <ThemeProvider theme={theme}>
      <AppRoutes></AppRoutes>

    </ThemeProvider>
    
  
  )
}

export default App
