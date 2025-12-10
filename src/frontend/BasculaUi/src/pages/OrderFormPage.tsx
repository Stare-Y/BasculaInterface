import { useSearchParams, useParams } from "react-router-dom";

export default function WeightDetailsPage() {
  const { id } = useParams();
  const [searchParams] = useSearchParams();


  return (
    <div>
      <h1>Peso {id}</h1>
    </div>
  );
}