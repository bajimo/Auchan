xof 0302txt 0032
Header {
 1;
 0;
 1;
}
template Header {
 <3D82AB43-62DA-11cf-AB39-0020AF71E433>
 WORD major;
 WORD minor;
 DWORD flags;
}

template Vector {
 <3D82AB5E-62DA-11cf-AB39-0020AF71E433>
 FLOAT x;
 FLOAT y;
 FLOAT z;
}

template Coords2d {
 <F6F23F44-7686-11cf-8F52-0040333594A3>
 FLOAT u;
 FLOAT v;
}

template Matrix4x4 {
 <F6F23F45-7686-11cf-8F52-0040333594A3>
 array FLOAT matrix[16];
}

template ColorRGBA {
 <35FF44E0-6C7C-11cf-8F52-0040333594A3>
 FLOAT red;
 FLOAT green;
 FLOAT blue;
 FLOAT alpha;
}

template ColorRGB {
 <D3E16E81-7835-11cf-8F52-0040333594A3>
 FLOAT red;
 FLOAT green;
 FLOAT blue;
}

template TextureFilename {
 <A42790E1-7810-11cf-8F52-0040333594A3>
 STRING filename;
}

template Material {
 <3D82AB4D-62DA-11cf-AB39-0020AF71E433>
 ColorRGBA faceColor;
 FLOAT power;
 ColorRGB specularColor;
 ColorRGB emissiveColor;
 [...]
}

template MeshFace {
 <3D82AB5F-62DA-11cf-AB39-0020AF71E433>
 DWORD nFaceVertexIndices;
 array DWORD faceVertexIndices[nFaceVertexIndices];
}

template MeshTextureCoords {
 <F6F23F40-7686-11cf-8F52-0040333594A3>
 DWORD nTextureCoords;
 array Coords2d textureCoords[nTextureCoords];
}

template MeshMaterialList {
 <F6F23F42-7686-11cf-8F52-0040333594A3>
 DWORD nMaterials;
 DWORD nFaceIndexes;
 array DWORD faceIndexes[nFaceIndexes];
 [Material]
}

template MeshNormals {
 <F6F23F43-7686-11cf-8F52-0040333594A3>
 DWORD nNormals;
 array Vector normals[nNormals];
 DWORD nFaceNormals;
 array MeshFace faceNormals[nFaceNormals];
}

template Mesh {
 <3D82AB44-62DA-11cf-AB39-0020AF71E433>
 DWORD nVertices;
 array Vector vertices[nVertices];
 DWORD nFaces;
 array MeshFace faces[nFaces];
 [...]
}

template FrameTransformMatrix {
 <F6F23F41-7686-11cf-8F52-0040333594A3>
 Matrix4x4 frameMatrix;
}

template Frame {
 <3D82AB46-62DA-11cf-AB39-0020AF71E433>
 [...]
}

Frame Mesh1_SorterTray1_M {
   FrameTransformMatrix {
0.010000,0.000000,0.000000,0.000000,
0.000000,0.010000,0.000000,0.000000,
0.000000,0.000000,0.010000,0.000000,
-5.008325,-0.837786,4.970976,1.000000;;
 }
Mesh Mesh1_SorterTray1_1 {
 134;
1.991748;21.620235;-370.437347;,
978.391541;35.407562;-276.281219;,
1001.664978;15.853959;-445.046387;,
978.391541;35.407562;-276.281219;,
1.991748;21.620235;-370.437347;,
28.355700;43.457600;-241.264999;,
978.391541;35.407562;-276.281219;,
50.558437;75.409119;-124.873909;,
947.669495;80.379410;-110.520859;,
28.355700;43.457600;-241.264999;,
947.669495;80.379410;-110.520859;,
74.691299;127.734001;1.634710;,
926.882996;127.734001;1.634710;,
50.558437;75.409119;-124.873909;,
74.691299;127.734001;1.634710;,
926.882996;147.733994;1.634710;,
926.882996;127.734001;1.634710;,
74.691299;147.733994;1.634710;,
947.669373;100.379402;-110.520889;,
926.882996;127.734001;1.634710;,
926.882996;147.733994;1.634710;,
947.669495;80.379410;-110.520859;,
978.391663;55.407562;-276.281219;,
947.669495;80.379410;-110.520859;,
947.669373;100.379402;-110.520889;,
978.391541;35.407562;-276.281219;,
1001.494019;38.595818;-409.515594;,
978.391541;35.407562;-276.281219;,
978.391663;55.407562;-276.281219;,
1001.664978;15.853959;-445.046387;,
1001.190430;36.300964;-549.143372;,
1001.664978;15.853959;-445.046387;,
1001.494019;38.595818;-409.515594;,
1001.190430;36.300964;-549.143372;,
1000.944763;19.388725;-585.219788;,
1001.664978;15.853959;-445.046387;,
949.008545;78.542084;-869.233215;,
924.859009;151.938004;-995.830017;,
924.859009;131.938004;-995.830017;,
949.009338;98.540894;-869.229309;,
979.319946;36.064976;-710.337280;,
949.009338;98.540894;-869.229309;,
949.008545;78.542084;-869.233215;,
979.320007;56.064968;-710.337219;,
1000.944763;19.388725;-585.219788;,
979.320007;56.064968;-710.337219;,
979.319946;36.064976;-710.337280;,
1001.190430;36.300964;-549.143372;,
1000.944763;19.388725;-585.219788;,
979.319946;36.064976;-710.337280;,
5.069408;23.333063;-631.237610;,
5.069408;23.333063;-631.237610;,
979.319946;36.064976;-710.337280;,
29.226700;45.613499;-752.807983;,
29.226700;45.613499;-752.807983;,
949.008545;78.542084;-869.233215;,
53.466164;83.632919;-883.593079;,
979.319946;36.064976;-710.337280;,
53.466164;83.632919;-883.593079;,
924.859009;131.938004;-995.830017;,
74.267998;131.938004;-995.830017;,
949.008545;78.542084;-869.233215;,
924.859009;131.938004;-995.830017;,
74.267998;151.938004;-995.830017;,
74.267998;131.938004;-995.830017;,
924.859009;151.938004;-995.830017;,
74.267998;131.938004;-995.830017;,
74.267998;151.938004;-995.830017;,
53.466164;83.632919;-883.593079;,
53.465416;103.631706;-883.589233;,
53.466164;83.632919;-883.593079;,
53.465416;103.631706;-883.589233;,
29.226700;45.613499;-752.807983;,
29.226700;65.613503;-752.807983;,
5.069408;23.333063;-631.237610;,
3.888880;43.266586;-630.721313;,
2.652309;41.651104;-370.152130;,
1.991748;21.620235;-370.437347;,
0.000000;35.619301;-497.015991;,
0.000000;15.619300;-497.015991;,
3.888880;43.266586;-630.721313;,
5.069408;23.333063;-631.237610;,
74.691299;147.733994;1.634710;,
74.691299;127.734001;1.634710;,
50.558441;95.409111;-124.873947;,
50.558437;75.409119;-124.873909;,
50.558441;95.409111;-124.873947;,
50.558437;75.409119;-124.873909;,
28.355700;63.457600;-241.264999;,
28.355700;43.457600;-241.264999;,
2.652309;41.651104;-370.152130;,
1.991748;21.620235;-370.437347;,
947.669373;100.379402;-110.520889;,
74.691299;147.733994;1.634710;,
926.882996;147.733994;1.634710;,
50.558441;95.409111;-124.873947;,
978.391663;55.407562;-276.281219;,
50.558441;95.409111;-124.873947;,
947.669373;100.379402;-110.520889;,
28.355700;63.457600;-241.264999;,
978.391663;55.407562;-276.281219;,
2.652309;41.651104;-370.152130;,
28.355700;63.457600;-241.264999;,
2.652309;41.651104;-370.152130;,
978.391663;55.407562;-276.281219;,
1001.494019;38.595818;-409.515594;,
0.000000;35.619301;-497.015991;,
1001.494019;38.595818;-409.515594;,
1001.190430;36.300964;-549.143372;,
2.652309;41.651104;-370.152130;,
3.888880;43.266586;-630.721313;,
0.000000;35.619301;-497.015991;,
1001.190430;36.300964;-549.143372;,
1001.190430;36.300964;-549.143372;,
979.320007;56.064968;-710.337219;,
3.888880;43.266586;-630.721313;,
3.888880;43.266586;-630.721313;,
979.320007;56.064968;-710.337219;,
29.226700;65.613503;-752.807983;,
29.226700;65.613503;-752.807983;,
949.009338;98.540894;-869.229309;,
53.465416;103.631706;-883.589233;,
979.320007;56.064968;-710.337219;,
53.465416;103.631706;-883.589233;,
924.859009;151.938004;-995.830017;,
74.267998;151.938004;-995.830017;,
949.009338;98.540894;-869.229309;,
5.069408;23.333063;-631.237610;,
0.000000;15.619300;-497.015991;,
1000.944763;19.388725;-585.219788;,
1001.664978;15.853959;-445.046387;,
0.000000;15.619300;-497.015991;,
1.991748;21.620235;-370.437347;,
1001.664978;15.853959;-445.046387;;

 64;
3;2,1,0;,
3;5,4,3;,
3;8,7,6;,
3;7,9,6;,
3;12,11,10;,
3;11,13,10;,
3;16,15,14;,
3;15,17,14;,
3;20,19,18;,
3;21,18,19;,
3;24,23,22;,
3;25,22,23;,
3;28,27,26;,
3;26,27,29;,
3;32,31,30;,
3;35,34,33;,
3;38,37,36;,
3;39,36,37;,
3;42,41,40;,
3;43,40,41;,
3;46,45,44;,
3;44,45,47;,
3;50,49,48;,
3;53,52,51;,
3;56,55,54;,
3;55,57,54;,
3;60,59,58;,
3;59,61,58;,
3;64,63,62;,
3;63,65,62;,
3;68,67,66;,
3;67,68,69;,
3;72,71,70;,
3;71,72,73;,
3;74,73,72;,
3;73,74,75;,
3;78,77,76;,
3;78,79,77;,
3;80,79,78;,
3;79,80,81;,
3;84,83,82;,
3;83,84,85;,
3;88,87,86;,
3;87,88,89;,
3;90,89,88;,
3;89,90,91;,
3;94,93,92;,
3;93,95,92;,
3;98,97,96;,
3;97,99,96;,
3;102,101,100;,
3;105,104,103;,
3;108,107,106;,
3;107,109,106;,
3;112,111,110;,
3;115,114,113;,
3;118,117,116;,
3;121,120,119;,
3;120,122,119;,
3;125,124,123;,
3;124,126,123;,
3;129,128,127;,
3;128,129,130;,
3;133,132,131;;
MeshMaterialList {
 1;
 64;
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0,
  0;;
Material Color_C08 {
 0.800000;0.556863;0.317647;1.000000;;
8.000000;
 0.132000;0.132000;0.132000;;
 0.000000;0.000000;0.000000;;
 }
}

 MeshNormals {
 134;
-0.000000;0.993698;-0.112087;,
-0.000000;0.982058;-0.188581;,
-0.000000;0.999155;-0.041109;,
-0.000000;0.982058;-0.188581;,
-0.000000;0.993698;-0.112087;,
-0.000000;0.975643;-0.219363;,
-0.000000;0.982058;-0.188581;,
-0.000000;0.945897;-0.324468;,
-0.000000;0.940993;-0.338426;,
-0.000000;0.975643;-0.219363;,
-0.000000;0.940993;-0.338426;,
-0.000000;0.917444;-0.397865;,
-0.000000;0.917444;-0.397865;,
-0.000000;0.945897;-0.324468;,
-0.000000;-0.000000;-1.000000;,
-0.000000;-0.000000;-1.000000;,
-0.000000;-0.000000;-1.000000;,
-0.000000;-0.000000;-1.000000;,
-0.983255;0.000000;-0.182236;,
-0.983255;0.000000;-0.182236;,
-0.983255;0.000000;-0.182236;,
-0.983255;0.000000;-0.182236;,
-0.981519;0.000000;-0.191365;,
-0.983255;0.000000;-0.182236;,
-0.983255;0.000000;-0.182236;,
-0.979575;0.000000;-0.201080;,
-0.999590;0.000000;-0.028625;,
-0.979575;0.000000;-0.201080;,
-0.981519;0.000000;-0.191365;,
-0.999856;-0.000000;0.016950;,
-0.999879;0.000000;-0.015571;,
-0.999856;-0.000000;0.016950;,
-0.999590;0.000000;-0.028625;,
-0.999879;0.000000;-0.015571;,
-0.999807;-0.000000;0.019639;,
-0.999856;-0.000000;0.016950;,
-0.982287;-0.000000;0.187381;,
-0.982287;-0.000000;0.187381;,
-0.982287;-0.000000;0.187381;,
-0.982287;-0.000000;0.187381;,
-0.980731;-0.000000;0.195362;,
-0.982287;-0.000000;0.187381;,
-0.982287;-0.000000;0.187381;,
-0.978995;-0.000000;0.203886;,
-0.999807;-0.000000;0.019639;,
-0.978995;-0.000000;0.203886;,
-0.980731;-0.000000;0.195362;,
-0.999879;0.000000;-0.015571;,
-0.000000;0.996701;0.081167;,
-0.000000;0.981772;0.190061;,
-0.000000;0.991823;0.127624;,
-0.000000;0.991823;0.127624;,
-0.000000;0.981772;0.190061;,
-0.000000;0.973874;0.227089;,
-0.000000;0.973874;0.227089;,
-0.000000;0.943485;0.331414;,
-0.000000;0.938511;0.345249;,
-0.000000;0.981772;0.190061;,
-0.000000;0.938511;0.345249;,
-0.000000;0.914699;0.404135;,
-0.000000;0.914699;0.404135;,
-0.000000;0.943485;0.331414;,
-0.000000;-0.000000;1.000000;,
-0.000000;-0.000000;1.000000;,
-0.000000;-0.000000;1.000000;,
-0.000000;-0.000000;1.000000;,
0.983255;-0.000000;0.182236;,
0.983255;-0.000000;0.182236;,
0.983255;-0.000000;0.182236;,
0.983255;-0.000000;0.182236;,
0.983255;-0.000000;0.182236;,
0.983255;-0.000000;0.182236;,
0.983255;-0.000000;0.182236;,
0.983255;-0.000000;0.182236;,
0.990551;-0.000000;0.137142;,
0.994048;-0.000000;0.108939;,
0.991834;0.000000;-0.127533;,
0.995416;0.000000;-0.095637;,
1.000000;-0.000000;-0.000000;,
1.000000;-0.000000;-0.000000;,
0.994048;-0.000000;0.108939;,
0.990551;-0.000000;0.137142;,
0.982287;0.000000;-0.187381;,
0.982287;0.000000;-0.187381;,
0.982287;0.000000;-0.187381;,
0.982287;0.000000;-0.187381;,
0.982287;0.000000;-0.187381;,
0.982287;0.000000;-0.187381;,
0.982287;0.000000;-0.187381;,
0.982287;0.000000;-0.187381;,
0.991834;0.000000;-0.127533;,
0.995416;0.000000;-0.095637;,
-0.000000;0.940993;-0.338426;,
-0.000000;0.917444;-0.397865;,
-0.000000;0.917444;-0.397865;,
-0.000000;0.945897;-0.324468;,
-0.000000;0.982058;-0.188581;,
-0.000000;0.945897;-0.324468;,
-0.000000;0.940993;-0.338426;,
-0.000000;0.975643;-0.219363;,
-0.000000;0.982058;-0.188581;,
-0.000000;0.993668;-0.112352;,
-0.000000;0.975643;-0.219363;,
-0.000000;0.993668;-0.112352;,
-0.000000;0.982058;-0.188581;,
-0.000000;0.997388;-0.072232;,
-0.000000;0.999991;0.004208;,
-0.000000;0.997388;-0.072232;,
-0.000000;0.998766;0.049663;,
-0.000000;0.993668;-0.112352;,
-0.000000;0.991877;0.127198;,
-0.000000;0.999991;0.004208;,
-0.000000;0.998766;0.049663;,
-0.000000;0.998766;0.049663;,
-0.000000;0.981772;0.190061;,
-0.000000;0.991877;0.127198;,
-0.000000;0.991877;0.127198;,
-0.000000;0.981772;0.190061;,
-0.000000;0.973874;0.227089;,
-0.000000;0.973874;0.227089;,
-0.000000;0.943486;0.331411;,
-0.000000;0.938512;0.345246;,
-0.000000;0.981772;0.190061;,
-0.000000;0.938512;0.345246;,
-0.000000;0.914699;0.404135;,
-0.000000;0.914699;0.404135;,
-0.000000;0.943486;0.331411;,
-0.000000;0.991823;0.127624;,
-0.000000;0.999991;0.004208;,
-0.000000;0.996701;0.081167;,
-0.000000;0.999155;-0.041109;,
-0.000000;0.999991;0.004208;,
-0.000000;0.993698;-0.112087;,
-0.000000;0.999155;-0.041109;;

 64;
3;2,1,0;,
3;5,4,3;,
3;8,7,6;,
3;7,9,6;,
3;12,11,10;,
3;11,13,10;,
3;16,15,14;,
3;15,17,14;,
3;20,19,18;,
3;21,18,19;,
3;24,23,22;,
3;25,22,23;,
3;28,27,26;,
3;26,27,29;,
3;32,31,30;,
3;35,34,33;,
3;38,37,36;,
3;39,36,37;,
3;42,41,40;,
3;43,40,41;,
3;46,45,44;,
3;44,45,47;,
3;50,49,48;,
3;53,52,51;,
3;56,55,54;,
3;55,57,54;,
3;60,59,58;,
3;59,61,58;,
3;64,63,62;,
3;63,65,62;,
3;68,67,66;,
3;67,68,69;,
3;72,71,70;,
3;71,72,73;,
3;74,73,72;,
3;73,74,75;,
3;78,77,76;,
3;78,79,77;,
3;80,79,78;,
3;79,80,81;,
3;84,83,82;,
3;83,84,85;,
3;88,87,86;,
3;87,88,89;,
3;90,89,88;,
3;89,90,91;,
3;94,93,92;,
3;93,95,92;,
3;98,97,96;,
3;97,99,96;,
3;102,101,100;,
3;105,104,103;,
3;108,107,106;,
3;107,109,106;,
3;112,111,110;,
3;115,114,113;,
3;118,117,116;,
3;121,120,119;,
3;120,122,119;,
3;125,124,123;,
3;124,126,123;,
3;129,128,127;,
3;128,129,130;,
3;133,132,131;;
 }
MeshTextureCoords {
 134;
0.482144;13.760200;,
38.880001;13.760200;,
39.370098;17.142700;,
38.880001;13.583800;,
0.482144;13.583800;,
1.116370;10.202240;,
38.263802;9.964180;,
1.739830;6.583340;,
37.658100;6.583340;,
1.116370;9.964180;,
37.066200;2.623970;,
2.940600;-0.756160;,
36.491402;-0.756160;,
2.349040;2.623970;,
2.940600;-3.413960;,
36.491402;-4.201360;,
36.491402;-3.413960;,
2.940600;-4.201360;,
9.803930;-2.856520;,
6.650040;-3.413960;,
6.650040;-4.201360;,
9.803930;-2.069120;,
16.375900;-0.883400;,
13.051900;-0.961070;,
13.051900;-1.748470;,
16.375900;-0.095990;,
22.446199;0.021670;,
19.757200;0.521275;,
19.757200;-0.266130;,
22.446199;0.809072;,
19.631901;0.212598;,
16.250299;0.887288;,
16.250299;0.099886;,
19.731400;0.208443;,
19.731400;0.995844;,
19.631901;1.000000;,
28.592800;-2.208480;,
31.751801;-4.366870;,
31.751801;-3.579470;,
28.592800;-2.995880;,
22.011499;-0.180870;,
25.340000;-1.860910;,
25.340000;-1.073500;,
22.011499;-0.968270;,
15.964200;0.771003;,
18.625900;-0.322980;,
18.625900;0.464424;,
15.964200;-0.016400;,
-39.370098;-21.873600;,
-38.871300;-25.259800;,
-0.534288;-25.259800;,
-0.534288;-24.995899;,
-38.236900;-28.383600;,
-1.150660;-28.383600;,
-1.150660;-27.964800;,
-37.613201;-31.354000;,
-1.756630;-31.354000;,
-38.236900;-27.964800;,
-2.348830;-33.378799;,
-36.411800;-36.771198;,
-2.923940;-36.771198;,
-37.003700;-33.378799;,
-36.411800;-3.579470;,
-2.923940;-4.366870;,
-2.923940;-3.579470;,
-36.411800;-4.366870;,
-39.145500;-3.579470;,
-39.145500;-4.366870;,
-35.989700;-2.208480;,
-35.989700;-2.995880;,
-32.740002;-1.073500;,
-32.740002;-1.860910;,
-29.414801;-0.180870;,
-29.414801;-0.968270;,
-26.032499;0.464424;,
-26.032499;-0.322980;,
-16.250299;0.099886;,
-16.250299;0.887288;,
-19.631901;0.212598;,
-19.631901;1.000000;,
-23.013700;0.071381;,
-23.013700;0.858782;,
0.551014;-4.201360;,
0.551014;-3.413960;,
-2.605980;-2.856520;,
-2.605980;-2.069120;,
-5.857180;-1.748470;,
-5.857180;-0.961070;,
-9.184400;-0.883400;,
-9.184400;-0.095990;,
-12.569100;-0.266130;,
-12.569100;0.521275;,
37.066200;2.310690;,
2.940600;-1.069440;,
36.491402;-1.069440;,
2.349040;2.310690;,
38.263802;9.762710;,
1.739830;6.381860;,
37.658100;6.381860;,
1.116370;9.762710;,
38.880001;13.440000;,
0.482144;13.440000;,
1.116370;10.058500;,
0.482144;13.675000;,
38.880001;13.675000;,
39.370098;17.057501;,
0.000000;20.594801;,
39.370098;17.211300;,
39.370098;20.594801;,
0.000000;17.211300;,
0.000000;-22.032499;,
0.000000;-18.647699;,
-39.370098;-22.032499;,
-39.370098;-21.965300;,
-38.871300;-25.351500;,
-0.534288;-25.351500;,
-0.534288;-25.145901;,
-38.236900;-28.533600;,
-1.150660;-28.533600;,
-1.150660;-28.172199;,
-37.613201;-31.561401;,
-1.756630;-31.561401;,
-38.236900;-28.172199;,
-2.348830;-33.696999;,
-36.411800;-37.089401;,
-2.923940;-37.089401;,
-37.003700;-33.696999;,
0.000000;-21.999599;,
0.000000;-18.614799;,
-39.370098;-21.999599;,
-39.370098;-18.614799;,
0.000000;20.621000;,
0.000000;17.237499;,
39.370098;17.237499;;
}
}
 }
