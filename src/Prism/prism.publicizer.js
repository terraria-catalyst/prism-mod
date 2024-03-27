import {createPublicizer} from "publicizer";

export const publicizer = createPublicizer("Prism");

publicizer.createAssembly("tModLoader").publicizeAll();
publicizer.createAssembly("FNA").publicizeAll();